using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Models;
using Api_TaskManager.Data;

namespace Api_TaskManager.Controllers;

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
        return await _context.TaskItems.Include(t => t.User).ToListAsync();
    }

    // GET: api/taskitem/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskItem>> GetTaskItem(int id)
    {
        var task = await _context.TaskItems.Include(t => t.User)
                                           .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();
        return task;
    }

    // POST: api/taskitem
    [HttpPost]
    public async Task<ActionResult<TaskItem>> CreateTaskItem(TaskItem taskItem)
    {
        _context.TaskItems.Add(taskItem);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTaskItem), new { id = taskItem.Id }, taskItem);
    }

    // PUT: api/taskitem/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTaskItem(int id, TaskItem taskItem)
    {
        if (id != taskItem.Id) return BadRequest();

        _context.Entry(taskItem).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/taskitem/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTaskItem(int id)
    {
        var task = await _context.TaskItems.FindAsync(id);
        if (task == null) return NotFound();

        _context.TaskItems.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
