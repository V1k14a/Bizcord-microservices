namespace Shared.Contracts.Events;

/// <summary>
/// Published when a new message is stored (integration event for other services).
/// </summary>
public class MessagePostedEvent
{
    public Guid MessageId { get; set; }

    public Guid ChannelId { get; set; }

    public Guid AuthorId { get; set; }

    public DateTime PostedAt { get; set; }
}
