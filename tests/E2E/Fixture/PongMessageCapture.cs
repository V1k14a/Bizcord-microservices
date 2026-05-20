using System.Threading.Channels;
using MessageClient.Interfaces;
using MessageClient.Types;
using Shared.Contracts.Messages;

namespace E2E.Tests.Fixture;

public class PongMessageCapture : IMessageHandler<PongMessage>
{
    private readonly Channel<PongMessage> _channel =
        Channel.CreateUnbounded<PongMessage>();

    public PongMessageCapture(IMessageClient messageClient)
    {
        // Unique ID per instance — same fix as MessageCapture<T>
        // Without this, multiple captures compete on the same queue
        // and only one receives each message
        var subscriptionId = new MessageSubscription(
            $"test-capture-PongMessage-{Guid.NewGuid()}");

        messageClient.SubscribeAsync<PongMessage>(subscriptionId, message =>
        {
            _channel.Writer.TryWrite(message);
        });
    }

    public Task Handle(PongMessage message, CancellationToken cancellationToken)
    {
        _channel.Writer.TryWrite(message);
        return Task.CompletedTask;
    }

    public async Task<PongMessage> WaitAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                "No PongMessage arrived. Did PongService receive and process the PingMessage?");
        }
    }
}