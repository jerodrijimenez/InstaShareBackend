using InstaShareUploadFileService.Models;
using Microsoft.EntityFrameworkCore;

namespace InstaShareUploadFileService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<UploadedFile> Files { get; set; }
    }
}
