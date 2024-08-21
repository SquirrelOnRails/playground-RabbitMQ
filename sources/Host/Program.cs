using Host.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var rabbitMQParams = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQParams>();
builder.Services.AddSingleton(rabbitMQParams);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapGet("/", () =>
{
    return "running...";
})
.WithName("root");
app.MapGet("/rabbitmq_single", () =>
{
    var client = new BL.Clients.RabbitMQClient(new Uri(rabbitMQParams.HostName), rabbitMQParams.QueueName);
});
app.MapGet("/rabbitmq_repeating", () =>
{
    var client = new BL.Clients.RabbitMQClient(new Uri(rabbitMQParams.HostName), rabbitMQParams.QueueName);

    new Thread(() =>
    {
        while (true)
        {
            client.SendMessage(new Random().Next(0, 100).ToString());
            Thread.Sleep(1000);
        }
    }).Start();
});

app.Run();
