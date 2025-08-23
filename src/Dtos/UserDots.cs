using System.ComponentModel.DataAnnotations;

namespace Api_TaskManager.Dtos
{
    // Create new User
    public class UserCreateDto
    {
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    // Read User from API
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<TaskItemReadDto> TaskItems { get; set; } = new();
    }
}
