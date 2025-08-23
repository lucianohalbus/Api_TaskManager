using System.ComponentModel.DataAnnotations;

namespace Api_TaskManager.Dtos
{
    public class TaskItemCreateDto
    {
        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool Completed { get; set; } = false;
    }

    public class TaskItemReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Completed { get; set; }

        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty; 
    }
}