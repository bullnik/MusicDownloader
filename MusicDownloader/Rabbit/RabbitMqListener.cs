using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using MusicDownloader.Models;

namespace MusicDownloader.Rabbit
{
    public class RabbitMqListener : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private string _requestQueueName = "MusicDownloadRequests";
        private string _resultsQueueName = "MusicDownloadResults";

        public RabbitMqListener()
        {
            Console.WriteLine("CREATING CONNECTION");
            var factory = GetConnectionFactory();
            _connection = factory.CreateConnection();
            Console.WriteLine("CONNECTION CREATED");
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: _requestQueueName, durable: false, 
                exclusive: false, autoDelete: false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine("RECEIVED MESSAGE " + content);
                Console.WriteLine("DOWNLOADING MUSIC " + content);
                DownloadMusic(content);
                Console.WriteLine("SENDING MESSAGE " + content);
                SendMessage(content);
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(_requestQueueName, false, consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }

        private void DownloadMusic(string link)
        {
            Downloader downloader = new();
            downloader.Download(link);
        }

        private void SendMessage(string message)
        {
            var factory = GetConnectionFactory();
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queue: _resultsQueueName,
                           durable: false,
                           exclusive: false,
                           autoDelete: false,
                           arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                           routingKey: _resultsQueueName,
                           basicProperties: null,
                           body: body);
        }

        private ConnectionFactory GetConnectionFactory()
        {
            return new ConnectionFactory
            {
                UserName = "bykrabbit",
                Password = "bykbykbyk",
                VirtualHost = "/",
                HostName = "rabbit",
                Port = AmqpTcpEndpoint.UseDefaultPort
            };
        }
    }
}
