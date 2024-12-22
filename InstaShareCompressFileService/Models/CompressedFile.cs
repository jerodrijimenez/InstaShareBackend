using System.ComponentModel.DataAnnotations;

namespace InstaShareCompressFileService.Models
{
    public class CompressedFile
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
        public string FilePath { get; set; }
    }
}
