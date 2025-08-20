namespace Api_TaskManager.Dtos;

public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}
