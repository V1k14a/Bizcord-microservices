using MessageClient.Interfaces;
using MessageClient.Types;

namespace MessagingMicroservice.Tests.Fixture;

/// <summary>
/// Captures published messages for contract and integration tests.
/// </summary>
public sealed class RecordingMessageClient : IMessageClient
{
    private readonly List<object> _published = new();
    private readonly object _gate = new();

    public IReadOnlyCollection<object> Published
    {
        get
        {
            lock (_gate)
                return _published.ToArray();
        }
    }

    public void Clear()
    {
        lock (_gate)
            _published.Clear();
    }

    public T? SinglePublished<T>()
    {
        lock (_gate)
            return _published.OfType<T>().LastOrDefault();
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
    {
        if (message is null)
            return Task.CompletedTask;
        lock (_gate)
            _published.Add(message);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<TMessage>(Action<TMessage> handler, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        Action<TMessage> handler,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task UnsubscribeAsync<TMessage>(
        MessageSubscription subscriptionId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
