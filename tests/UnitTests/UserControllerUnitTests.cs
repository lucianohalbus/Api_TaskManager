using Xunit;
using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Controllers;
using Api_TaskManager.Data;
using Api_TaskManager.Models;
using Api_TaskManager.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace UnitTests
{
    public class UserControllerUnitTests
    {
        private DbContextOptions<ApplicationDbContext> CreateNewContextOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // ← Banco único por teste
                .Options;
        }

        // Helper para criar usuários válidos
        private User CreateTestUser(int id, string username, string email, string password = "123456", string role = "User")
        {
            return new User
            {
                Id = id,
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            };
        }

        private ClaimsPrincipal CreateFakeUser(int userId, string role = "User")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task GetUsers_ReturnsPagedResult()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            context.Users.Add(CreateTestUser(1, "user1", "user1@test.com"));
            context.Users.Add(CreateTestUser(2, "user2", "user2@test.com"));
            await context.SaveChangesAsync();

            var controller = new UserController(context);
            var result = await controller.GetUsers(1, 10);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var pagedResult = Assert.IsType<PagedResultDto<UserReadDto>>(okResult.Value);

            Assert.Equal(2, pagedResult.TotalCount);
        }

        [Fact]
        public async Task GetUser_ReturnsUser_WhenExists()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var user = CreateTestUser(3, "user3", "user3@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context);
            var result = await controller.GetUser(user.Id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<UserReadDto>(okResult.Value);
            Assert.Equal("user3", dto.Username);
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var controller = new UserController(context);

            var result = await controller.GetUser(999);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateUser_ReturnsCreatedUser()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var controller = new UserController(context);

            var dto = new UserCreateDto
            {
                Username = "newuser",
                Email = "newuser@test.com",
                Password = "password123"
            };

            var result = await controller.CreateUser(dto);

            var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
            var readDto = Assert.IsType<UserReadDto>(createdAt.Value);

            Assert.Equal("newuser", readDto.Username);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNoContent_WhenSuccessful()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var user = CreateTestUser(10, "oldname", "old@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(user.Id)
                    }
                }
            };

            var updatedUser = CreateTestUser(user.Id, "updated", "updated@test.com");

            var result = await controller.UpdateUser(user.Id, updatedUser);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsUnauthorized_WhenNoUserLoggedIn()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var controller = new UserController(context);

            var updatedUser = CreateTestUser(20, "nouser", "nouser@test.com");

            var result = await controller.UpdateUser(20, updatedUser);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(1, "User")
                    }
                }
            };

            var updatedUser = CreateTestUser(999, "ghost", "ghost@test.com");

            var result = await controller.UpdateUser(999, updatedUser);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateUser_ReturnsForbid_WhenUserTriesToUpdateAnotherUser_AndNotAdmin()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var user = CreateTestUser(50, "target", "target@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(99, "User") // outro usuário, não admin
                    }
                }
            };

            var updatedUser = CreateTestUser(user.Id, "hacker", "hacker@test.com");

            var result = await controller.UpdateUser(user.Id, updatedUser);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNoContent_WhenSuccessful()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var user = CreateTestUser(30, "todelete", "delete@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(user.Id)
                    }
                }
            };

            var result = await controller.DeleteUser(user.Id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsUnauthorized_WhenNoUserLoggedIn()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var user = CreateTestUser(40, "unauth", "unauth@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context);

            var result = await controller.DeleteUser(user.Id);

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(1, "Admin")
                    }
                }
            };

            var result = await controller.DeleteUser(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsForbid_WhenDeletingAdmin()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var admin = CreateTestUser(60, "admin", "admin@test.com", role: "Admin");
            context.Users.Add(admin);
            await context.SaveChangesAsync();

            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(1, "Admin") // admin tentando deletar outro admin
                    }
                }
            };

            var result = await controller.DeleteUser(admin.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ReturnsForbid_WhenUserTriesToDeleteAnotherUser_AndNotAdmin()
        {
            using var context = new ApplicationDbContext(CreateNewContextOptions());
            var target = CreateTestUser(70, "victim", "victim@test.com");
            context.Users.Add(target);
            await context.SaveChangesAsync();

            var controller = new UserController(context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = CreateFakeUser(99, "User") // outro user, sem permissão
                    }
                }
            };

            var result = await controller.DeleteUser(target.Id);

            Assert.IsType<ForbidResult>(result);
        }
    }
}