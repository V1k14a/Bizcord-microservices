using MessageClient.Interfaces;
using Shared.Contracts.Messages;
    
namespace Bizcord.Handlers;

public class PingMessageHandlerV3 : IMessageHandler<PingMessage>
{
    public Task Handle(PingMessage message, CancellationToken cancellationToken)
    {
        Console.WriteLine("Ping message received V3");
        return Task.CompletedTask;
    }
}