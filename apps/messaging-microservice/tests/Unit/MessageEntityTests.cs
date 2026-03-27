using MessagingMicroservice.Domain;
using MessagingMicroservice.Domain.Entities;
using MessagingMicroservice.Domain.ValueObjects;
using Xunit;

namespace MessagingMicroservice.Tests.Unit;

public class MessageEntityTests
{
    [Fact]
    public void Create_with_valid_content_produces_message_with_identity_and_timestamps()
    {
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var content = MessageContent.Create("hello");

        var message = Message.Create(channelId, authorId, content);

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(channelId, message.ChannelId);
        Assert.Equal(authorId, message.AuthorId);
        Assert.Equal("hello", message.Content);
        Assert.True(message.PostedAt <= DateTime.UtcNow);
        Assert.Null(message.UpdatedAt);
    }

    [Fact]
    public void MessageContent_Create_throws_when_empty()
    {
        Assert.Throws<DomainException>(() => MessageContent.Create(" "));
    }

    [Fact]
    public void UpdateContent_sets_content_and_UpdatedAt()
    {
        var message = Message.Create(Guid.NewGuid(), Guid.NewGuid(), MessageContent.Create("a"));
        var before = message.PostedAt;

        message.UpdateContent(MessageContent.Create("b"));

        Assert.Equal("b", message.Content);
        Assert.NotNull(message.UpdatedAt);
        Assert.True(message.UpdatedAt >= before);
    }
}
