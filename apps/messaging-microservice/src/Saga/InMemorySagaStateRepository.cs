using System.Collections.Concurrent;

namespace MessagingMicroservice.Saga;

public sealed class InMemorySagaStateRepository : ISagaStateRepository
{
    private readonly ConcurrentDictionary<Guid, MessagePostSagaState> _states = new();

    public Task<MessagePostSagaState?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_states.TryGetValue(sagaId, out var s) ? s : null);

    public Task SaveAsync(MessagePostSagaState state, CancellationToken cancellationToken = default)
    {
        _states[state.SagaId] = state;
        return Task.CompletedTask;
    }
}
