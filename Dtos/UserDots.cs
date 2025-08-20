namespace Api_TaskManager.Dtos;

// Create new User
public class UserCreateDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}

// Read User from api
public class UserReadDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<TaskItemReadDto> TaskItems { get; set; } = new();
}
