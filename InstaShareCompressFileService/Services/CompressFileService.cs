using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using IronZip;
using InstaShareCompressFileService.Models;
using System.Text.Json;

namespace InstaShareCompressFileService.Services
{
    public class CompressFileService
    {
        private readonly string _uploadPath;
        private readonly string _compressedPath;

        public CompressFileService(string uploadPath, string compressedPath)
        {
            _uploadPath = uploadPath;
            _compressedPath = compressedPath;

            StartListening();
        }

        //Listening for rabbitmq messages
        private async void StartListening()
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
                await ProcessFileAsync(message);  
            };

            await channel.BasicConsumeAsync("to_compress_file_queue", autoAck: true, consumer: consumer);
        }

        //Compress the file
        private async Task ProcessFileAsync(string JsonMessage)
        {
            var file = JsonSerializer.Deserialize<CompressedFile>(JsonMessage);
            var compressedFilePath = Path.Combine(_compressedPath, file.Name + ".zip");

            using (var archive = new IronZipArchive())
            {
                archive.Add(file.FilePath);
                archive.SaveAs(compressedFilePath);
            }

            file.FilePath = compressedFilePath;

            var jsonMessage = JsonSerializer.Serialize(file);

            await PublishCompressToRabbitMQAsync(jsonMessage);

            await PublishRemoveToRabbitMQAsync(file.Id.ToString());
        }

        //Send the message with the file metadata to RabbitMq
        private async Task PublishCompressToRabbitMQAsync(string jsonObject)
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_add_compress_file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(jsonObject);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "to_add_compress_file_queue", body: body);
        }

        //Send the message with the file remove order to RabbitMq
        private async Task PublishRemoveToRabbitMQAsync(string fileId)
        {
            var factory = new ConnectionFactory { HostName = "rabbitmq" };

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "to_add_remove_file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(fileId);

            await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "to_add_remove_file_queue", body: body);
        }
    }
}
