namespace Shared.Contracts.Messages;

public class PingMessage
{
    public required string Message { get; set; }
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}