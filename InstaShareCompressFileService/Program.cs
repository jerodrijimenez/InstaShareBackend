using InstaShareCompressFileService.Services;

namespace InstaShareCompressFileService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddScoped<CompressFileService>(provider =>
            {
                var uploadPath = builder.Configuration["Paths:UploadFiles"];
                var compressedPath = builder.Configuration["Paths:CompressedFiles"];
                return new CompressFileService(uploadPath, compressedPath);
            });

            var app = builder.Build();

            app.Run();
        }
    }
}
