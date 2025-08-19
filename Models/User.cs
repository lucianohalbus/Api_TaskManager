namespace Api_TaskManager.Models;

public class User
{
    public int Id { get; set; }

    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }

    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
}