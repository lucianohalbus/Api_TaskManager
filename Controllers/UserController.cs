using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Models;
using Api_TaskManager.Data;
using Api_TaskManager.Dtos;

namespace Api_TaskManager.Controllers;

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

        // Converting User to DTOs
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
                Completed = t.Completed
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
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(UserCreateDto dto)
    {
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = dto.Password
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
    public async Task<IActionResult> UpdateUser(int id, User user)
    {
        if (id != user.Id) return BadRequest();

        _context.Entry(user).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/user/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
