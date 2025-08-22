using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Api_TaskManager.Controllers;
using Api_TaskManager.Data;
using Api_TaskManager.Models;
using Api_TaskManager.Dtos;
using System.Security.Claims;

namespace UnitTests
{
    public class TaskItemControllerUnitTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly TaskItemController _controller;

        public TaskItemControllerUnitTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _controller = new TaskItemController(_context);
        }

        public void Dispose() => _context.Dispose();

        private void SetUser(int userId, bool isAdmin = false)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            var identity = new ClaimsIdentity(claims, "mock");
            _controller.ControllerContext.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };
        }

        // ============ GET ALL ============

        [Fact]
        public async Task GetTaskItems_ReturnsEmpty_WhenNoTasks()
        {
            var result = await _controller.GetTaskItems();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<PagedResultDto<TaskItemReadDto>>(ok.Value);
            Assert.Empty(dto.Items);
        }

        [Fact]
        public async Task GetTaskItems_ReturnsPagedResult()
        {
            var user = new User { Username = "user", Email = "u@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.TaskItems.AddRange(
                new TaskItem { Title = "T1", Description = "D1", UserId = user.Id },
                new TaskItem { Title = "T2", Description = "D2", UserId = user.Id }
            );
            await _context.SaveChangesAsync();

            var result = await _controller.GetTaskItems(page: 1, pageSize: 1);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<PagedResultDto<TaskItemReadDto>>(ok.Value);
            Assert.Single(dto.Items);
            Assert.Equal(2, dto.TotalCount);
        }

        // ============ GET ONE ============

        [Fact]
        public async Task GetTaskItem_ReturnsNotFound_WhenMissing()
        {
            var result = await _controller.GetTaskItem(999);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetTaskItem_ReturnsOk_WhenExists()
        {
            var user = new User { Username = "u", Email = "e@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "Task", Description = "Desc", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            var result = await _controller.GetTaskItem(task.Id);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<TaskItemReadDto>(ok.Value);
            Assert.Equal(task.Title, dto.Title);
        }

        // ============ CREATE ============

        [Fact]
        public async Task CreateTaskItem_ReturnsUnauthorized_WhenNoUser()
        {
            var dto = new TaskItemCreateDto { Title = "T", Description = "D", Completed = false };
            var result = await _controller.CreateTaskItem(dto);
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task CreateTaskItem_CreatesSuccessfully()
        {
            var user = new User { Username = "u", Email = "e@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            SetUser(user.Id);

            var dto = new TaskItemCreateDto { Title = "T1", Description = "D1", Completed = true };
            var result = await _controller.CreateTaskItem(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var readDto = Assert.IsType<TaskItemReadDto>(created.Value);
            Assert.Equal("T1", readDto.Title);
            Assert.True(readDto.Completed);
        }

        // ============ UPDATE ============

        [Fact]
        public async Task UpdateTaskItem_ReturnsUnauthorized_WhenNoUser()
        {
            var dto = new TaskItemCreateDto { Title = "T", Description = "D", Completed = false };
            var result = await _controller.UpdateTaskItem(1, dto);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task UpdateTaskItem_ReturnsNotFound_WhenMissing()
        {
            SetUser(1);
            var dto = new TaskItemCreateDto { Title = "T", Description = "D", Completed = false };
            var result = await _controller.UpdateTaskItem(999, dto);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateTaskItem_ReturnsForbid_WhenUserNotOwnerOrAdmin()
        {
            var user = new User { Username = "owner", Email = "o@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "T", Description = "D", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            SetUser(999); // outro user
            var dto = new TaskItemCreateDto { Title = "T2", Description = "D2", Completed = true };

            var result = await _controller.UpdateTaskItem(task.Id, dto);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateTaskItem_UpdatesSuccessfully_WhenOwner()
        {
            var user = new User { Username = "u", Email = "e@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "T", Description = "D", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            SetUser(user.Id);
            var dto = new TaskItemCreateDto { Title = "Updated", Description = "DD", Completed = true };

            var result = await _controller.UpdateTaskItem(task.Id, dto);
            Assert.IsType<NoContentResult>(result);

            var updated = await _context.TaskItems.FindAsync(task.Id);
            Assert.Equal("Updated", updated!.Title);
            Assert.True(updated.Completed);
        }

        [Fact]
        public async Task UpdateTaskItem_AllowsAdmin()
        {
            var user = new User { Username = "owner", Email = "o@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "Old", Description = "D", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            SetUser(999, isAdmin: true);
            var dto = new TaskItemCreateDto { Title = "New", Description = "DD", Completed = false };

            var result = await _controller.UpdateTaskItem(task.Id, dto);
            Assert.IsType<NoContentResult>(result);
        }

        // ============ DELETE ============

        [Fact]
        public async Task DeleteTaskItem_ReturnsUnauthorized_WhenNoUser()
        {
            var result = await _controller.DeleteTaskItem(1);
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task DeleteTaskItem_ReturnsNotFound_WhenMissing()
        {
            SetUser(1);
            var result = await _controller.DeleteTaskItem(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteTaskItem_ReturnsForbid_WhenUserNotOwnerOrAdmin()
        {
            var user = new User { Username = "owner", Email = "o@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "T", Description = "D", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            SetUser(999); // não é owner nem admin
            var result = await _controller.DeleteTaskItem(task.Id);
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteTaskItem_DeletesSuccessfully_WhenOwner()
        {
            var user = new User { Username = "u", Email = "e@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "T", Description = "D", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            SetUser(user.Id);
            var result = await _controller.DeleteTaskItem(task.Id);
            Assert.IsType<NoContentResult>(result);

            var exists = await _context.TaskItems.FindAsync(task.Id);
            Assert.Null(exists);
        }

        [Fact]
        public async Task DeleteTaskItem_AllowsAdmin()
        {
            var user = new User { Username = "owner", Email = "o@test.com", PasswordHash = "h" };
            _context.Users.Add(user);
            var task = new TaskItem { Title = "T", Description = "D", User = user };
            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            SetUser(999, isAdmin: true);
            var result = await _controller.DeleteTaskItem(task.Id);
            Assert.IsType<NoContentResult>(result);
        }
    }
}
