using MessageClient.Types;

namespace MessageClient.Interfaces;

/// <summary>
/// Application-facing messaging API. Implementations hide broker wiring (EasyNetQ, exchanges, serializers).
/// </summary>
public interface IMessageClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Publishes a message to the default pub/sub exchange for <typeparamref name="TMessage"/>.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes using a subscription id derived from <typeparamref name="TMessage"/> (stable across restarts for the same type).
    /// Prefer <see cref="SubscribeAsync{TMessage}(MessageSubscription, Action{TMessage}, CancellationToken)"/> when you need isolation (e.g. tests or competing consumers).
    /// </summary>
    Task SubscribeAsync<TMessage>(Action<TMessage> handler, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes with an explicit subscription id. Each unique id gets its own queue for that message type.
    /// </summary>
    Task SubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        Action<TMessage> handler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a subscription created with the same <paramref name="subscriptionId"/> and message type.
    /// </summary>
    Task UnsubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        CancellationToken cancellationToken = default);
}
