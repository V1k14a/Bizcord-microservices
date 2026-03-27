using MessagingMicroservice.Domain.Entities;

namespace MessagingMicroservice.Infrastructure;

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Message message, CancellationToken cancellationToken = default);

    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
