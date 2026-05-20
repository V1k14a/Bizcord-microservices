using MessageClient.Interfaces;
using MessageClient.Types;

namespace PongMicroservice.Tests.Fixture;

public class FakeMessageClient : IMessageClient
{
    private readonly List<object> _published = new();

    public IReadOnlyList<object> Published => _published;

    public T? SinglePublished<T>() => _published.OfType<T>().SingleOrDefault();

    public IEnumerable<T> AllPublished<T>() => _published.OfType<T>();

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
    {
        if (message is not null)
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
