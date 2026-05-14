using MessageClient.Configuration;
using MessageClient.Factories;
using MessageClient.Implementation;
using MessageClient.Interfaces;
using MessagingMicroservice.Application;
using MessagingMicroservice.Handlers;
using MessagingMicroservice.Infrastructure;
using MessagingMicroservice.Security;
using MessagingMicroservice.Saga;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

await VaultBootstrap.ApplyIfConfiguredAsync(builder.Configuration);

builder.Services.AddMessagingJwtAuthentication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT with symmetric signing key (same as API gateway). Include role claim: User or Service."
        });
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
});

builder.Services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();
builder.Services.AddSingleton<ISagaStateRepository, InMemorySagaStateRepository>();
builder.Services.AddScoped<MessagesService>();
builder.Services.AddSingleton<MessagePostSagaOrchestrator>();

var rabbitConnection =
    builder.Configuration["RabbitMQ:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("RabbitMQ__ConnectionString")
    ?? "host=localhost";

builder.Services.AddSingleton(new RabbitMqClientOptions { ConnectionString = rabbitConnection });
builder.Services.AddSingleton<IMessageClient>(sp =>
{
    var opts = sp.GetRequiredService<RabbitMqClientOptions>();
    var inner = RabbitMqFactory.CreateMessageClient(opts);
    return new ResilientMessageClient(inner, sp.GetRequiredService<ILogger<ResilientMessageClient>>());
});
builder.Services.AddSingleton<MessageHandlerRegistry>();
builder.Services.AddHostedService<MessageBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var urls = app.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
if (urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();

public partial class Program { }
