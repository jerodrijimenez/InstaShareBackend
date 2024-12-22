using System.ComponentModel.DataAnnotations;

namespace InstaShareFileService.Models
{
    public class FileMetadata
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UploadedBy { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public long Size { get; set; }

        [Required]
        public string Format { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string CompressedFilePath { get; set; }
    }
}
