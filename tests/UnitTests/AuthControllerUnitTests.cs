using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Controllers;
using Api_TaskManager.Data;
using Api_TaskManager.Models;
using Api_TaskManager.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace UnitTests
{
    public class AuthControllerUnitTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly IConfiguration _config;

        public AuthControllerUnitTests()
        {
            _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var inMemorySettings = new Dictionary<string, string>
            {
                {"Jwt:Key", "SuperSecretTestKey_ChangeMe_1234567890_ABCDEFG"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"},
                {"Jwt:ExpireMinutes", "60"}
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        private async Task SeedUser(ApplicationDbContext context, string email, string password, string role = "User")
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            context.Users.Add(new User
            {
                Username = "testuser",
                Email = email,
                PasswordHash = hashedPassword,
                Role = role
            });
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Login_ReturnsToken_WhenCredentialsAreValid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedUser(context, "valid@test.com", "password123");

            var controller = new AuthController(context, _config);
            var result = await controller.Login(new LoginDto { Email = "valid@test.com", Password = "password123" });

            var okResult = Assert.IsType<ActionResult<LoginResponseDto>>(result);
            var response = Assert.IsType<LoginResponseDto>(okResult.Value);

            Assert.False(string.IsNullOrEmpty(response.Token));
            Assert.False(string.IsNullOrEmpty(response.RefreshToken));
            Assert.True(response.Expiration > DateTime.UtcNow);
        }

        [Fact]
        public async Task Login_ThrowsUnauthorized_WhenEmailDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            var controller = new AuthController(context, _config);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await controller.Login(new LoginDto { Email = "notfound@test.com", Password = "password123" })
            );
        }
        [Fact]
        public async Task Login_ThrowsUnauthorizedAccessException_WhenPasswordIsInvalid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedUser(context, "valid@test.com", "password123");

            var controller = new AuthController(context, _config);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                controller.Login(new LoginDto { Email = "valid@test.com", Password = "wrongpass" })
            );

            Assert.Equal("Invalid username or password", ex.Message);
        }
        [Fact]
        public async Task Refresh_ReturnsNewToken_WhenRefreshTokenIsValid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedUser(context, "valid@test.com", "password123");

            var controller = new AuthController(context, _config);
            var loginResult = await controller.Login(new LoginDto { Email = "valid@test.com", Password = "password123" });
            var loginResponse = (loginResult.Value as LoginResponseDto)!;

            var refreshResult = await controller.Refresh(new TokenRequestDto { RefreshToken = loginResponse.RefreshToken });

            var okResult = Assert.IsType<ActionResult<LoginResponseDto>>(refreshResult);
            var response = Assert.IsType<LoginResponseDto>(okResult.Value);

            Assert.False(string.IsNullOrEmpty(response.Token));
            Assert.False(string.IsNullOrEmpty(response.RefreshToken));
            Assert.True(response.Expiration > DateTime.UtcNow);
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenTokenIsInvalid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedUser(context, "valid@test.com", "password123");

            var controller = new AuthController(context, _config);
            var result = await controller.Refresh(new TokenRequestDto { RefreshToken = "invalidToken" });

            var actionResult = Assert.IsType<ActionResult<LoginResponseDto>>(result);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);

            Assert.Equal(401, unauthorized.StatusCode);
            Assert.Equal("Invalid or expired refresh token", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenEmailDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            // não adiciona usuário no banco
            var controller = new AuthController(context, _config);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                controller.Login(new LoginDto { Email = "notfound@test.com", Password = "any" })
            );

            Assert.Equal("Invalid username or password", ex.Message);
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenRefreshTokenIsInvalid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedUser(context, "valid@test.com", "password123");

            var controller = new AuthController(context, _config);

            var result = await controller.Refresh(new TokenRequestDto { RefreshToken = "invalid-refresh-token" });

            var actionResult = Assert.IsType<ActionResult<LoginResponseDto>>(result);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);

            Assert.Equal(401, unauthorized.StatusCode);
            Assert.Equal("Invalid or expired refresh token", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ThrowsException_WhenJwtKeyIsInvalid()
        {
            using var context = new ApplicationDbContext(_dbOptions);
            await SeedUser(context, "valid@test.com", "password123");

            // cria configuração inválida (chave menor que 256 bits)
            var invalidConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Jwt:Key", "shortkey" },
                    { "Jwt:Issuer", "test-issuer" },
                    { "Jwt:Audience", "test-audience" }
                })
                .Build();

            var controller = new AuthController(context, invalidConfig);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                controller.Login(new LoginDto { Email = "valid@test.com", Password = "password123" })
            );
        }
    }
}
