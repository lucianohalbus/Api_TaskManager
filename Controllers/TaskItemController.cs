using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Models;
using Api_TaskManager.Data;
using Api_TaskManager.Dtos;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Api_TaskManager.Extensions;

namespace Api_TaskManager.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class TaskItemController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TaskItemController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/taskitem
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskItem>>> GetTaskItems()
    {
        var tasks = await _context.TaskItems
            .Include(t => t.User)
            .ToListAsync();

        var taskDtos = tasks.Select(t => new TaskItemReadDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Completed = t.Completed,
            UserId = t.UserId,
            Username = t.User.Username // from navigation property
        }).ToList();

        return Ok(taskDtos);
    }

    // GET: api/taskitem/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetTaskItem(int id)
    {
        var task = await _context.TaskItems
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        var dto = new TaskItemReadDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Completed = task.Completed,
            UserId = task.UserId,
            Username = task.User.Username
        };

        return Ok(dto);
    }

    // POST: api/taskitem
    [HttpPost]
    public async Task<ActionResult<TaskItemReadDto>> CreateTaskItem(TaskItemCreateDto dto)
    {
        // Get UserId from JWT
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Completed = dto.Completed,
            UserId = userId.Value
        };

        _context.TaskItems.Add(task);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(userId.Value);

        var readDto = new TaskItemReadDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Completed = task.Completed,
            UserId = task.UserId,
            Username = user?.Username ?? string.Empty
        };

        return CreatedAtAction(nameof(GetTaskItem), new { id = task.Id }, readDto);
    }

    // PUT: api/taskitem/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTaskItem(int id, TaskItemCreateDto dto)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var task = await _context.TaskItems.FindAsync(id);
        if (task == null) return NotFound();

        // Ensure the logged user owns the task
        if (task.UserId != userId.Value) return Forbid();

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Completed = dto.Completed;

        _context.Entry(task).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/taskitem/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTaskItem(int id)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var task = await _context.TaskItems.FindAsync(id);
        if (task == null) return NotFound();

        // Ensure the logged user owns the task
        if (task.UserId != userId.Value) return Forbid();

        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}