using System.ComponentModel.DataAnnotations;

namespace InstaShareAuthService.Models
{
    public class UserLoginDto
    {
        [Required]
        public required string UserName { get; set; }

        [Required]
        public required string UserPass { get; set; }
    }
}
