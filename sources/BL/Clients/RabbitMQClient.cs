using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Linq;
using System.Text;
using System.Threading.Channels;

namespace BL.Clients
{
    public class RabbitMQClient : IDisposable
    {
        private protected ConnectionFactory _factory;
        private protected IConnection _connection;
        private protected IModel _channel;
        private protected EventingBasicConsumer _consumer;
        private protected string _exchangeName;

        public readonly Guid ClientId = Guid.NewGuid();
        public EventingBasicConsumer Consumer { get { return _consumer; } }

        public RabbitMQClient(Uri hostName, string queueName, string exchangeName = "custom_exchange")
        {
            // Setup
            _factory = new ConnectionFactory()
            {
                HostName = hostName.Host,
                Port = hostName.Port,
            };

            _exchangeName = exchangeName;

            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct);

            // Declare queue
            _channel.QueueDeclare(queue: queueName,
                     durable: true,
                     exclusive: false,
                     autoDelete: false,
                     arguments: null);

            // Bind queue to exchange with necessary routing keys
            _channel.QueueBind(queue: queueName,
                           exchange: _exchangeName,
                           routingKey: "client_initialization");
            _channel.QueueBind(queue: queueName,
                           exchange: _exchangeName,
                           routingKey: "send_message");

            // Setup consumer
            _consumer = new EventingBasicConsumer(_channel);
            _consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                if (ea.RoutingKey == "client_initialization" && message == ClientId.ToString())
                {
                    Console.WriteLine($"[EVT={ea.RoutingKey}] {ClientId}: acknowledged");
                }
                else if (ea.RoutingKey == "send_message")
                {
                    Console.WriteLine($"[EVT={ea.RoutingKey}] {ClientId}: acknowledged: {message}");
                }
            };

            _channel.BasicConsume(queue: queueName,
                     autoAck: true,
                     consumer: _consumer);

            // Send client initialization message
            var initBody = Encoding.UTF8.GetBytes($"{ClientId}");
            _channel.BasicPublish(exchange: _exchangeName,
                     routingKey: "client_initialization",
                     basicProperties: null,
                     body: initBody);

            Console.WriteLine($"[SELF] {ClientId}: client initialization request sent...");
        }

        public void SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: _exchangeName,
                     routingKey: "send_message",
                     basicProperties: null,
                     body: body);

            Console.WriteLine($"[send_message] {ClientId}: message sent: {message}");
        }

        public void Dispose()
        {
            _channel.Dispose();
            _connection.Dispose();
            Console.WriteLine($"[SELF] {ClientId}: disposed");
        }
    }
}
