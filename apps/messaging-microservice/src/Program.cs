using MessageClient.Configuration;
using MessageClient.Extension;
using MessagingMicroservice.Application;
using MessagingMicroservice.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();
builder.Services.AddScoped<MessagesService>();

var rabbitConnection =
    builder.Configuration["RabbitMQ:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("RabbitMQ__ConnectionString")
    ?? "host=localhost";

builder.Services.AddRabbitMqMessageClient(new RabbitMqClientOptions { ConnectionString = rabbitConnection });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Docker / compose often bind HTTP only (no TLS on 8080); avoid redirect loops in the browser.
var urls = app.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
if (urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.MapControllers();
app.Run();

public partial class Program { }
