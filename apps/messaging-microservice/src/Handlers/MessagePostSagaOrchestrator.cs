using MessageClient.Interfaces;
using MessagingMicroservice.Domain;
using MessagingMicroservice.Domain.Entities;
using MessagingMicroservice.Domain.ValueObjects;
using MessagingMicroservice.Infrastructure;
using MessagingMicroservice.Saga;
using Shared.Contracts.Events;
using Shared.Contracts.Sagas;

namespace MessagingMicroservice.Handlers;

/// <summary>
/// Orchestrated saga: persist message → publish integration event → compensate by deleting the message if publish fails.
/// </summary>
public class MessagePostSagaOrchestrator :
    IMessageHandler<InitiateMessagePost>,
    IMessageHandler<MessagePersisted>,
    IMessageHandler<MessagePostedPublishFailed>
{
    private readonly IMessageClient _messageClient;
    private readonly ISagaStateRepository _sagaRepo;
    private readonly IMessageRepository _messages;
    private readonly ILogger<MessagePostSagaOrchestrator> _logger;

    public MessagePostSagaOrchestrator(
        IMessageClient messageClient,
        ISagaStateRepository sagaRepo,
        IMessageRepository messages,
        ILogger<MessagePostSagaOrchestrator> logger)
    {
        _messageClient = messageClient;
        _sagaRepo = sagaRepo;
        _messages = messages;
        _logger = logger;
    }

    public async Task Handle(InitiateMessagePost message, CancellationToken cancellationToken)
    {
        var state = new MessagePostSagaState { SagaId = message.SagaId };
        await _sagaRepo.SaveAsync(state, cancellationToken);

        Message entity;
        try
        {
            var body = MessageContent.Create(message.Content);
            entity = Message.Create(message.ChannelId, message.AuthorId, body);
            await _messages.AddAsync(entity, cancellationToken);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Saga {SagaId}: invalid message content", message.SagaId);
            state.IsFailed = true;
            state.CompletedAt = DateTime.UtcNow;
            await _sagaRepo.SaveAsync(state, cancellationToken);
            return;
        }

        state.MessageId = entity.Id;
        state.MessagePersisted = true;
        await _sagaRepo.SaveAsync(state, cancellationToken);

        await _messageClient.PublishAsync(
            new MessagePersisted
            {
                SagaId = message.SagaId,
                MessageId = entity.Id,
                ChannelId = entity.ChannelId,
                AuthorId = entity.AuthorId,
                PostedAt = entity.PostedAt
            },
            cancellationToken);
    }

    public async Task Handle(MessagePersisted message, CancellationToken cancellationToken)
    {
        var state = await _sagaRepo.GetByIdAsync(message.SagaId, cancellationToken);
        if (state is null || state.EventPublished || state.IsFailed)
            return;

        try
        {
            await _messageClient.PublishAsync(
                new MessagePostedEvent
                {
                    MessageId = message.MessageId,
                    ChannelId = message.ChannelId,
                    AuthorId = message.AuthorId,
                    PostedAt = message.PostedAt
                },
                cancellationToken);

            state.EventPublished = true;
            state.IsCompleted = true;
            state.CompletedAt = DateTime.UtcNow;
            await _sagaRepo.SaveAsync(state, cancellationToken);

            await _messageClient.PublishAsync(
                new MessagePostSagaCompleted { SagaId = message.SagaId, MessageId = message.MessageId },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga {SagaId}: failed to publish MessagePostedEvent", message.SagaId);
            await _messageClient.PublishAsync(
                new MessagePostedPublishFailed { SagaId = message.SagaId, Reason = ex.Message },
                cancellationToken);
        }
    }

    public async Task Handle(MessagePostedPublishFailed message, CancellationToken cancellationToken)
    {
        var state = await _sagaRepo.GetByIdAsync(message.SagaId, cancellationToken);
        if (state is null || !state.MessageId.HasValue || state.IsFailed)
            return;

        await _messages.DeleteAsync(state.MessageId.Value, cancellationToken);

        state.IsFailed = true;
        state.EventPublished = false;
        state.CompletedAt = DateTime.UtcNow;
        await _sagaRepo.SaveAsync(state, cancellationToken);

        _logger.LogWarning(
            "Saga {SagaId}: compensated by deleting message {MessageId}. Reason: {Reason}",
            message.SagaId,
            state.MessageId,
            message.Reason);
    }
}
