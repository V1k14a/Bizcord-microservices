using MessageClient.Configuration;
using MessageClient.Extension;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRabbitMqMessageClient(
    new RabbitMqClientOptions
    {
        ConnectionString = builder.Configuration["RabbitMQ__ConnectionString"] ?? "host=rabbitmq"
    },
    new MessageHandlerOptions { SubscriptionPrefix = "pong" }
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }