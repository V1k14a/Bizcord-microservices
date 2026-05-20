namespace MessagingMicroservice.Saga;

public interface ISagaStateRepository
{
    Task<MessagePostSagaState?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default);

    Task SaveAsync(MessagePostSagaState state, CancellationToken cancellationToken = default);
}
