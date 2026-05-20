namespace Shared.Contracts.Sagas;

/// <summary>
/// Cross-service saga messages for posting a message: persist, publish integration event, compensate on publish failure.
/// </summary>
public class InitiateMessagePost
{
    public Guid SagaId { get; set; }

    public Guid ChannelId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = null!;
}

public class MessagePersisted
{
    public Guid SagaId { get; set; }

    public Guid MessageId { get; set; }

    public Guid ChannelId { get; set; }

    public Guid AuthorId { get; set; }

    public DateTime PostedAt { get; set; }
}

public class MessagePostedPublishFailed
{
    public Guid SagaId { get; set; }

    public string Reason { get; set; } = null!;
}

public class MessagePostSagaCompleted
{
    public Guid SagaId { get; set; }

    public Guid MessageId { get; set; }
}
