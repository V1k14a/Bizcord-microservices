using EasyNetQ;
using MessageClient.Interfaces;
using MessageClient.Types;

namespace MessageClient.Adapters;

public sealed class RabbitMqAdapter(IBus bus) : IAdapter
{
    private readonly Dictionary<string, IDisposable> _subscriptions = new();
    private readonly object _gate = new();
    private bool _disposed;

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await bus.PubSub.PublishAsync(message!, cancellationToken).ConfigureAwait(false);
    }

    public async Task SubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        Action<TMessage> handler,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var key = SubscriptionKey<TMessage>(subscriptionId);

        lock (_gate)
        {
            if (_subscriptions.Remove(key, out var existing))
                existing.Dispose();
        }

        var handle = await bus.PubSub
            .SubscribeAsync(subscriptionId.Subscription, handler, cancellationToken)
            .ConfigureAwait(false);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _subscriptions[key] = handle;
        }
    }

    public Task UnsubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var key = SubscriptionKey<TMessage>(subscriptionId);

        IDisposable? handle;
        lock (_gate)
        {
            _subscriptions.Remove(key, out handle);
        }

        handle?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        lock (_gate)
        {
            foreach (var sub in _subscriptions.Values)
                sub.Dispose();
            _subscriptions.Clear();
        }

        bus.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static string SubscriptionKey<TMessage>(MessageSubscription subscriptionId) =>
        $"{subscriptionId.Subscription}\0{typeof(TMessage).FullName}";
}
