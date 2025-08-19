namespace Api_TaskManager.Models;

public class TaskItem
{
    public int Id { get; set; }

    public required string Title { get; set; }  
    public string Description { get; set; } = string.Empty; 
    public bool Completed { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!; 
}
