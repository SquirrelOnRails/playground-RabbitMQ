using MassTransit;
using MassTransit.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL.Clients
{
    public static class ReservedQueues
    {
        public const string INITIALIZATION = "initialization_queue";
        public const string MAIN = "main_queue";
        public const string TEXT_MESSAGE = "text_message_queue";
    }

    public class MassTransitClient : IDisposable
    {
        private readonly Uri _hostUri;
        private readonly string _queueName;

        private readonly IBusControl _busControl;
        
        public readonly Guid ClientId = Guid.NewGuid();

        public MassTransitClient(Uri hostUri, string userName, string password, string queueName = ReservedQueues.MAIN)
        {
            Console.WriteLine($"[SELF] {ClientId}: initialization start");

            _hostUri = hostUri;
            _queueName = queueName;

            _busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(_hostUri.Host, h => 
                {
                    h.Username(userName ?? "guest");
                    h.Password(password ?? "guest");
                });

                cfg.ReceiveEndpoint(ReservedQueues.INITIALIZATION, ep =>
                {
                    ep.Handler<InitializationMessage>(context =>
                    {
                        var message = context.Message;

                        if (message.ClientId == ClientId)
                        {
                            Console.WriteLine($"[EVT={ReservedQueues.INITIALIZATION}] {ClientId}: acknowledged");
                        }

                        return Task.CompletedTask;
                    });
                });

                cfg.ReceiveEndpoint(ReservedQueues.TEXT_MESSAGE, ep =>
                {
                    ep.Handler<TextMessage>(context =>
                    {
                        var message = context.Message;
                        Console.WriteLine($"[EVT={ReservedQueues.TEXT_MESSAGE}] {ClientId}: acknowledged: {message.Text}");

                        return Task.CompletedTask;
                    });
                });
            });

            _busControl.Start();
            Console.WriteLine($"[{ReservedQueues.INITIALIZATION}] {ClientId}: client initialization...");
            SendInitializationMessage();
        }

        public async Task SendInitializationMessage()
        {
            var message = new InitializationMessage { ClientId = ClientId };
            await _busControl.Publish(message);

            Console.WriteLine($"[{ReservedQueues.INITIALIZATION}] {ClientId}: message sent...");
        }

        public async Task TextMessage(string text)
        {
            var message = new TextMessage { Text = text };
            await _busControl.Publish(message);

            Console.WriteLine($"[{ReservedQueues.TEXT_MESSAGE}] {ClientId}: message sent... : {text}");
        }

        public void Dispose()
        {
            _busControl.Stop();
            Console.WriteLine($"[SELF] {ClientId}: disposed");
        }
    }

    public class InitializationMessage
    {
        public Guid ClientId { get; set; }
    }

    public class TextMessage
    {
        public string Text { get; set; }
    }
}
