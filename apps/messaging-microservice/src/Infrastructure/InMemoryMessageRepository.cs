using System.Collections.Concurrent;
using MessagingMicroservice.Domain.Entities;

namespace MessagingMicroservice.Infrastructure;

public sealed class InMemoryMessageRepository : IMessageRepository
{
    private readonly ConcurrentDictionary<Guid, Message> _messages = new();

    public Task<IReadOnlyList<Message>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Message>>(
            _messages.Values.OrderByDescending(m => m.PostedAt).ToList());

    public Task<Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_messages.TryGetValue(id, out var m) ? m : null);

    public Task<IReadOnlyList<Message>> GetByChannelIdAsync(Guid channelId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Message>>(
            _messages.Values
                .Where(m => m.ChannelId == channelId)
                .OrderByDescending(m => m.PostedAt)
                .ToList());

    public Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (!_messages.TryAdd(message.Id, message))
            throw new InvalidOperationException($"Message {message.Id} already exists.");
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Message message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_messages.TryRemove(id, out _));
}
