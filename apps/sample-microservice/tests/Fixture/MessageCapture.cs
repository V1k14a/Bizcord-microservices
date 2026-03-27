using System.Threading.Channels;
using MessageClient.Interfaces;
using MessageClient.Types;

namespace SampleMicroservice.Tests.Fixture;

public class MessageCapture<T> : IAsyncDisposable
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public MessageCapture(IMessageClient messageClient)
    {
        // Unique ID per instance — each capture gets its own queue on the broker
        var subscriptionId = new MessageSubscription($"test-capture-{typeof(T).Name}-{Guid.NewGuid()}");

        messageClient.SubscribeAsync<T>(subscriptionId, message =>
        {
            _channel.Writer.TryWrite(message);
        });
    }

    public async Task<T> WaitForMessageAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));

        try
        {
            return await _channel.Reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(
                $"No message of type {typeof(T).Name} arrived within the timeout. " +
                $"Check that the service published it and the routing key matches.");
        }
    }

    public ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        return ValueTask.CompletedTask;
    }
}