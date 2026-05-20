using MessageClient.Interfaces;
using Shared.Contracts.Messages;
    
namespace Bizcord.Handlers;

public class PingMessageHandlerV2 : IMessageHandler<PingMessage>
{
    public Task Handle(PingMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine("Ping message received V2");
        return Task.CompletedTask;
    }
}