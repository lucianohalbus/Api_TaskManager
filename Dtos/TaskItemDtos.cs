namespace Api_TaskManager.Dtos;

// Crate TaskItem
public class TaskItemCreateDto
{
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Completed { get; set; } = false;
    public int UserId { get; set; } // vincular ao usuário
}

// Read TaskItem
public class TaskItemReadDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Completed { get; set; }
}
