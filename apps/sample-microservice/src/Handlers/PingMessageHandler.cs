using MessageClient.Interfaces;
using MessageClient.Types;
using Shared.Contracts.Messages; 

namespace src.Handlers;

public class PingMessageHandler : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    
    public PingMessageHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private async Task<IMessageClient> SetupMessageClient()
    {
        using var scope = _scopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMessageClient>();
    }

    private void HandlePingMessage(PingMessage message)
    {
        Console.WriteLine("Message received!");
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Setting up message handler");
        IMessageClient messageClient = await SetupMessageClient();

        await messageClient.SubscribeAsync<PingMessage>(
            HandlePingMessage
        );
        
        Console.WriteLine("MessageBackgroundService is running");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
        Console.WriteLine("MessageBackgroundService is stopping");
    }
}