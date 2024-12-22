using InstaShareUploadFileService.Data;
using InstaShareUploadFileService.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RabbitMQ.Client.Events;
using Docker.DotNet.Models;

namespace InstaShareUploadFileService.Services
{
    public class FileUploadService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadPath;

        public FileUploadService(ApplicationDbContext context, string uploadPath)
        {
            _context = context;
            _uploadPath = uploadPath;

            StartListeningRename();
            StartListeningRemove();
        }

        //Get all uploaded Files
        public async Task<IEnumerable<UploadedFile>> GetFiles()
        {
            return await _context.Files.ToListAsync();
        }

        //Get one uploaded File
        public async Task<UploadedFile> GetFilesId(int id)
        {
            return await _context.Files.FindAsync(id);
        }

        //Gets all files uploaded by the user
        public async Task<IEnumerable<UploadedFile>> GetFilesUser(int userId)
        {
            return await _context.Files.Where(f => f.UploadedBy == userId).ToListAsync();
        }

        //Upload file
        public async Task UploadFileAsync(FileUploadDto fileUploadDto, int userId)
        {
            var file = fileUploadDto.File;
            var fileName = GetUniqueFileName(file.FileName);
            var filePath = Path.Combine(_uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var newFile = new UploadedFile
            {
                UploadedBy = userId,
                Name = fileName,
                Size = file.Length,
                Format = Path.GetExtension(file.FileName),
                Status = "Uploading and Compressing",
                Path = filePath
            };

            _context.Files.Add(newFile);
            await _context.SaveChangesAsync();

            var jsonFile = _context.Files.SingleOrDefaultAsync(f => f.Name == fileName);
            var jsonMessage = JsonSerializer.Serialize(jsonFile);

            await PublishToRabbitMQAsync(jsonMessage);
        }

        //Get a unique name for the file
        private string GetUniqueFileName(string originalFileName)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var extension = Path.GetExtension(originalFileName);
            var uniqueFileName = originalFileName;
            var counter = 1;

            while (File.Exists(Path.Combine(_uploadPath, uniqueFileName)))
            {
                uniqueFileName = $"{fileNameWithoutExtension}({counter}){extension}";
                counter++;
            }

            return uniqueFileName;
        }

        //Send the message with the file metadata to RabbitMq
        private async Task PublishToRabbitMQAsync(string jsonObject)
        {
            var factory = new ConnectionFactory{HostName = "rabbitmq"};

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_compress_file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(jsonObject);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "to_compress_file_queue", body: body);
        }

        //Listening for rabbitmq messages
        private async void StartListeningRename()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_compress_file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await ProcessRenameFileAsync(message);
            };

            await channel.BasicConsumeAsync("to_compress_file_queue", autoAck: true, consumer: consumer);
        }

        //Upload renamed file
        private async Task ProcessRenameFileAsync(string message)
        {
            var file = JsonSerializer.Deserialize<UploadedFile>(message);

            _context.Files.Add(file);
            await _context.SaveChangesAsync();

            await PublishToRabbitMQAsync(message);
        }

        //Listening for rabbitmq messages
        private async void StartListeningRemove()
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_compress_file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                await ProcessRemoveFileAsync(message);
            };

            await channel.BasicConsumeAsync("to_compress_file_queue", autoAck: true, consumer: consumer);
        }


        //Remove a file uploaded
        private async Task ProcessRemoveFileAsync(string message)
        {
            int fileId = Convert.ToInt32(message);

            var file = await _context.Files.FindAsync(fileId);
            if (file == null) throw new UnauthorizedAccessException("File not found");

            var filePath = file.Path;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.Files.Remove(file);
            await _context.SaveChangesAsync();
        }
    }
}
