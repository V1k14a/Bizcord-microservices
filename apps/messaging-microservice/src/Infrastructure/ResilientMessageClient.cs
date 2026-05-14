using MessageClient.Interfaces;
using MessageClient.Types;
using Polly;
using Polly.Timeout;

namespace MessagingMicroservice.Infrastructure;

/// <summary>
/// Retries and times out broker publishes to mitigate transient RabbitMQ failures (see FAILURE_POINTS.md).
/// </summary>
public sealed class ResilientMessageClient : IMessageClient
{
    private readonly IMessageClient _inner;
    private readonly ILogger<ResilientMessageClient> _logger;
    private readonly AsyncPolicy _publishPolicy;

    public ResilientMessageClient(IMessageClient inner, ILogger<ResilientMessageClient> logger)
    {
        _inner = inner;
        _logger = logger;
        var timeout = Policy.TimeoutAsync(TimeSpan.FromSeconds(15), TimeoutStrategy.Pessimistic);
        var retry = Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                3,
                attempt => TimeSpan.FromMilliseconds(150 * Math.Pow(2, attempt)),
                (ex, delay, attempt, _) =>
                {
                    _logger.LogWarning(ex, "Message publish attempt {Attempt} failed; retrying in {Delay}ms", attempt, delay.TotalMilliseconds);
                });
        _publishPolicy = Policy.WrapAsync(timeout, retry);
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) =>
        _publishPolicy.ExecuteAsync(ct => _inner.PublishAsync(message, ct), cancellationToken);

    public Task SubscribeAsync<TMessage>(Action<TMessage> handler, CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(handler, cancellationToken);

    public Task SubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        Action<TMessage> handler,
        CancellationToken cancellationToken = default) =>
        _inner.SubscribeAsync(subscriptionId, handler, cancellationToken);

    public Task UnsubscribeAsync<TMessage>(MessageSubscription subscriptionId, CancellationToken cancellationToken = default) =>
        _inner.UnsubscribeAsync<TMessage>(subscriptionId, cancellationToken);

    public void Dispose() => _inner.Dispose();

    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
