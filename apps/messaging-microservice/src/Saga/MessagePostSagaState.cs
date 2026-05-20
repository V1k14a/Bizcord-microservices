namespace MessagingMicroservice.Saga;

public class MessagePostSagaState
{
    public Guid SagaId { get; set; }

    public Guid? MessageId { get; set; }

    public bool MessagePersisted { get; set; }

    public bool EventPublished { get; set; }

    public bool IsCompleted { get; set; }

    public bool IsFailed { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
