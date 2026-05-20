using MessageClient.Interfaces;
using MessageClient.Types;

namespace MessageClient.Implementation;

public sealed class MessageClient(IAdapter adapter) : IMessageClient
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) =>
        adapter.PublishAsync(message, cancellationToken);

    public Task SubscribeAsync<TMessage>(Action<TMessage> handler, CancellationToken cancellationToken = default)
    {
        var subscriptionId = new MessageSubscription(typeof(TMessage).FullName ?? typeof(TMessage).Name);
        return adapter.SubscribeAsync(subscriptionId, handler, cancellationToken);
    }

    public Task SubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        Action<TMessage> handler,
        CancellationToken cancellationToken = default) =>
        adapter.SubscribeAsync(subscriptionId, handler, cancellationToken);

    public Task UnsubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        CancellationToken cancellationToken = default) =>
        adapter.UnsubscribeAsync<TMessage>(subscriptionId, cancellationToken);

    public void Dispose() => adapter.Dispose();

    public ValueTask DisposeAsync() => adapter.DisposeAsync();
}
