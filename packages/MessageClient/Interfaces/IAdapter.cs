using MessageClient.Types;

namespace MessageClient.Interfaces;

public interface IAdapter : IDisposable, IAsyncDisposable
{
    Task SubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        Action<TMessage> handler,
        CancellationToken cancellationToken = default);

    Task UnsubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        CancellationToken cancellationToken = default);

    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);
}
