using System.ComponentModel.DataAnnotations;

namespace InstaShareUploadFileService.Models
{
    public class FileUploadDto
    {
        [Required]
        public required IFormFile File { get; set; }
    }
}
