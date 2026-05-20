namespace Shared.Contracts.Messages;

/// <summary>
/// Cross-service data contract for a chat message (no domain logic).
/// </summary>
public class MessageDto
{
    public Guid Id { get; set; }

    public Guid ChannelId { get; set; }

    public Guid AuthorId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime PostedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
