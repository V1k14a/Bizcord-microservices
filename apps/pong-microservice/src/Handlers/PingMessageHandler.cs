using MessageClient.Interfaces;
using Shared.Contracts.Messages;

namespace PongMicroservice.Handlers;

// Discovered automatically by MessageHandlerRegistry via reflection.
// Receives a PingMessage and publishes a PongMessage in reply.
public class PingMessageHandler : IMessageHandler<PingMessage>
{
    private readonly IMessageClient _messageClient;
    private readonly ILogger<PingMessageHandler> _logger;

    public PingMessageHandler(IMessageClient messageClient, ILogger<PingMessageHandler> logger)
    {
        _messageClient = messageClient;
        _logger = logger;
    }

    public async Task Handle(PingMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PongService received ping: {Message} (correlationId: {Id})",
            message.Message, message.CorrelationId);

        var pong = new PongMessage
        {
            Message = "pong",
            CorrelationId = message.CorrelationId,  // Echo back so caller can correlate
            RepliedAt = DateTime.UtcNow
        };

        await _messageClient.PublishAsync(pong);

        _logger.LogInformation("PongService published pong for correlationId: {Id}", pong.CorrelationId);
    }
}