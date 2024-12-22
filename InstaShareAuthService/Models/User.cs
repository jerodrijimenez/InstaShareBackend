using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InstaShareAuthService.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        public required string UserName { get; set; }

        [Required]
        public required string UserEmail { get; set; }

        [Required]
        public required string UserPass { get; set; }
    }
}
