using MessagingMicroservice.Domain.ValueObjects;

namespace MessagingMicroservice.Domain.Entities;

public class Message
{
    public Guid Id { get; private set; }

    public Guid ChannelId { get; private set; }

    public Guid AuthorId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime PostedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public static Message Create(Guid channelId, Guid authorId, MessageContent content) =>
        new()
        {
            Id = Guid.NewGuid(),
            ChannelId = channelId,
            AuthorId = authorId,
            Content = content.Value,
            PostedAt = DateTime.UtcNow
        };

    public void UpdateContent(MessageContent content)
    {
        Content = content.Value;
        UpdatedAt = DateTime.UtcNow;
    }
}
