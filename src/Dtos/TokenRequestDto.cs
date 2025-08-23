using System.ComponentModel.DataAnnotations;

namespace Api_TaskManager.Dtos
{
    public class TokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }  
}



