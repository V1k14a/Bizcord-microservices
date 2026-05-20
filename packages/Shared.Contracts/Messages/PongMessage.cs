namespace Shared.Contracts.Messages;

public class PongMessage
{
    public required string Message { get; set; }
    public Guid CorrelationId { get; set; } 
    public DateTime RepliedAt { get; set; } = DateTime.UtcNow;
}