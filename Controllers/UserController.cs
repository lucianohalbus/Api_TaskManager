using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Models;
using Api_TaskManager.Data;
using Api_TaskManager.Dtos;
using Microsoft.AspNetCore.Authorization;
using Api_TaskManager.Extensions;

namespace Api_TaskManager.Controllers;

[Authorize(Policy = "UserOrAdmin")]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/user
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers()
    {
        var users = await _context.Users.Include(u => u.TaskItems).ToListAsync();

        var userDtos = users.Select(u => new UserReadDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            TaskItems = u.TaskItems.Select(t => new TaskItemReadDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Completed = t.Completed,
                UserId = t.UserId,
                Username = t.User != null ? t.User.Username : string.Empty
            }).ToList()
        });

        return Ok(userDtos);
    }

    // GET: api/user/5
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _context.Users.Include(u => u.TaskItems)
                                       .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();
        
        var dto = new UserReadDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            TaskItems = user.TaskItems.Select(t => new TaskItemReadDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Completed = t.Completed
            }).ToList()
        };

        return Ok(dto);
    }

    // POST: api/user
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UserReadDto>> CreateUser(UserCreateDto dto)
    {
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var readDto = new UserReadDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            TaskItems = new List<TaskItemReadDto>()
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, readDto);
    }

    // PUT: api/user/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, User updateUser)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && user.Id != userId.Value)
            return Forbid();

        user.Username = updateUser.Username;
        user.Email = updateUser.Email;

        if (!string.IsNullOrEmpty(updateUser.PasswordHash))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUser.PasswordHash);
        
        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/user/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");

        if (user.Role == "Admin")
            return Forbid(); 

        if (!isAdmin && user.Id != userId.Value)
            return Forbid();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}