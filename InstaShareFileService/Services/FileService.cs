using Docker.DotNet.Models;
using InstaShareFileService.Data;
using InstaShareFileService.Models;
using IronZip;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InstaShareFileService.Services
{
    public class FileService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _compressedFilesPath;
        private readonly string _uploadsFilesPath;

        public FileService(ApplicationDbContext context, string compressedFilesPath, string uploadFilesPath)
        {
            _context = context;
            _compressedFilesPath = compressedFilesPath;
            _uploadsFilesPath = uploadFilesPath;

            StartListening();
        }

        //Get all Compressed Files
        public async Task<IEnumerable<FileMetadata>> GetFiles()
        {
            return await _context.Files.ToListAsync();
        }

        //Get one Compressed File
        public async Task<FileMetadata> GetFilesId(int id)
        {
            return await _context.Files.FindAsync(id);
        }

        //Gets all compressed files uploaded by the user
        public async Task<IEnumerable<FileMetadata>> GetFilesUser(int userId)
        {
            return await _context.Files
                            .Where(f => f.UploadedBy == userId)
                            .ToListAsync();
        }

        //Download a selected File
        public async Task<IActionResult> DownloadFile(String fileNames)
        {
            string nameF = fileNames;
            var file = await _context.Files.SingleOrDefaultAsync(f => f.Name == nameF);
            if (file == null) { return null; }

            var filePath = file.CompressedFilePath;
            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            var contentType = "application/zip";
            var fileName = System.IO.Path.GetFileName(filePath);

            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = fileName
            };
        }

        //Change the file name
        public async Task RenameFile(int userId, string oldName, string newName)
        {
            var file = await _context.Files.SingleOrDefaultAsync(f => f.Name == oldName && f.UploadedBy == userId);
            if (file == null) throw new UnauthorizedAccessException("File not found or not owned by user.");

            var zipPath = Path.Combine(_compressedFilesPath, oldName, ".zip");

            ExtractFile(zipPath);

            var oldPath = Path.Combine(_compressedFilesPath, oldName, file.Format);
            var newPath = Path.Combine(_uploadsFilesPath, newName, file.Format);

            if (File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
            }

            var jsonFile = new FileMetadata();

            jsonFile.Name = newName;
            jsonFile.Format = file.Format;
            jsonFile.CompressedFilePath = newPath;
            jsonFile.Size = file.Size;
            jsonFile.Status = "Renaming the file";
            jsonFile.UploadedBy = userId;

            var jsonMessage = JsonSerializer.Serialize(jsonFile);

            await PublishToRabbitMQAsync (jsonMessage);

            await DeleteFile(userId, oldName);

             await _context.SaveChangesAsync();
        }

        //Extract zip file
        public  void ExtractFile(String zipPath)
        {
            IronZipArchive.ExtractArchiveToDirectory(zipPath, _compressedFilesPath);
        }

        //publish to a rabbitmq queue
        private async Task PublishToRabbitMQAsync(string jsonObject)
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_change_name_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(jsonObject);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "to_change_name_queue", body: body);
        }

        //Listening for rabbitmq messages
        private async void StartListening()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_add_compress_file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await PostFileInDb(message);
            };

            await channel.BasicConsumeAsync("to_add_compress_file_queue", autoAck: true, consumer: consumer);
        }

        //Post a file in DBS
        public async Task PostFileInDb(string JsonMessage)
        {
            var file = JsonSerializer.Deserialize<FileMetadata>(JsonMessage);

            if (_context.Files.Any(u => u.Name == file.Name))
                file.Name= GetUniqueFileName(file.Name);

            _context.Files.Add(file);
            await _context.SaveChangesAsync();
        }

        //Get unique name for compressed file
        private string GetUniqueFileName(string originalFileName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var uniqueFileName = originalFileName;
            var counter = 1;

            while (File.Exists(Path.Combine(_compressedFilesPath, uniqueFileName)))
            {
                uniqueFileName = $"{fileNameWithoutExtension}({counter}){extension}";
                counter++;
            }

            return uniqueFileName;
        }

        //Delete a file uploaded and compressed
        public async Task DeleteFile(int userId, string fileName)
        {
            var file = await _context.Files.SingleOrDefaultAsync(f => f.Name == fileName && f.UploadedBy == userId);
            if (file == null) throw new UnauthorizedAccessException("File not found or not owned by user.");

            var filePath = file.CompressedFilePath;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.Files.Remove(file);
            await _context.SaveChangesAsync();
        }
    }
}
