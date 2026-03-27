using MessagingMicroservice.Domain;
using MessagingMicroservice.Domain.Entities;
using MessagingMicroservice.Domain.ValueObjects;
using MessagingMicroservice.Infrastructure;
using MessageClient.Interfaces;
using Shared.Contracts.Events;
using Shared.Contracts.Messages;

namespace MessagingMicroservice.Application;

public class MessagesService
{
    private readonly IMessageRepository _repository;
    private readonly IMessageClient _messageClient;

    public MessagesService(IMessageRepository repository, IMessageClient messageClient)
    {
        _repository = repository;
        _messageClient = messageClient;
    }

    public async Task<MessageDto> CreateAsync(
        Guid channelId,
        Guid authorId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var body = MessageContent.Create(content);
        var message = Message.Create(channelId, authorId, body);
        await _repository.AddAsync(message, cancellationToken);

        await _messageClient.PublishAsync(
            new MessagePostedEvent
            {
                MessageId = message.Id,
                ChannelId = message.ChannelId,
                AuthorId = message.AuthorId,
                PostedAt = message.PostedAt
            },
            cancellationToken);

        return MapToDto(message);
    }

    public async Task<IReadOnlyList<MessageDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<MessageDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _repository.GetByIdAsync(id, cancellationToken);
        return message is null ? null : MapToDto(message);
    }

    public async Task<MessageDto?> UpdateAsync(
        Guid id,
        string content,
        CancellationToken cancellationToken = default)
    {
        var message = await _repository.GetByIdAsync(id, cancellationToken);
        if (message is null)
            return null;

        var body = MessageContent.Create(content);
        message.UpdateContent(body);
        await _repository.UpdateAsync(message, cancellationToken);
        return MapToDto(message);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _repository.DeleteAsync(id, cancellationToken);

    private static MessageDto MapToDto(Message message) =>
        new()
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            AuthorId = message.AuthorId,
            Content = message.Content,
            PostedAt = message.PostedAt,
            UpdatedAt = message.UpdatedAt
        };
}
