using MessageClient.Interfaces;
using Microsoft.Extensions.Hosting;

namespace MessageClient.Implementation;

public class MessageBackgroundService : BackgroundService, IMessageBackgroundService
{
    private readonly MessageHandlerRegistry _messageHandlerRegistry;
    private readonly IMessageClient _messageClient;
    
    public MessageBackgroundService(MessageHandlerRegistry messageHandlerRegistry, IMessageClient messageClient)
    {
        _messageHandlerRegistry = messageHandlerRegistry;
        _messageClient = messageClient;
    }   
    public void StartListening()
    {
        Console.WriteLine("Starting to listen to messages.");
        _messageHandlerRegistry.RegisterAllHandlers(
            _messageClient
        );
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartListening();
        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}