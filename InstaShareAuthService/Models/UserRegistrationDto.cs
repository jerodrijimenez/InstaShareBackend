using System.ComponentModel.DataAnnotations;

namespace InstaShareAuthService.Models
{
    public class UserRegistrationDto
    {
        [Required]
        public required string UserName { get; set; }

        [Required]
        public required string UserEmail { get; set; }

        [Required]
        public required string UserPass { get; set; }
    }
}
