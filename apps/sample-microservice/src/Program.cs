using MessageClient.Extension;
using MessageClient.Configuration;
using src.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRabbitMqMessageClient(
    new RabbitMqClientOptions { ConnectionString = "host=rabbitmq" }, 
    new MessageHandlerOptions { SubscriptionPrefix = "ping"}
);
// builder.Services.AddHostedService<PingMessageHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }