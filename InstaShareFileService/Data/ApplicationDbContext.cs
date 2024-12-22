using InstaShareFileService.Models;
using Microsoft.EntityFrameworkCore;

namespace InstaShareFileService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<FileMetadata> Files { get; set; }
    }
}
