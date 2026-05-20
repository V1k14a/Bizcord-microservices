using FluentAssertions;
using MessagingMicroservice.Application;
using MessagingMicroservice.Infrastructure;
using MessagingMicroservice.Tests.Fixture;
using MessageClient.Interfaces;
using Shared.Contracts.Events;
using Xunit;

namespace MessagingMicroservice.Tests.Contract;

public class MessagePostedContractTests
{
    [Fact]
    public async Task After_create_service_publishes_MessagePostedEvent_with_matching_ids()
    {
        var repo = new InMemoryMessageRepository();
        var recording = new RecordingMessageClient();
        IMessageClient client = recording;
        var service = new MessagesService(repo, client);

        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var dto = await service.CreateAsync(channelId, authorId, "hello world");

        var published = recording.SinglePublished<MessagePostedEvent>();
        published.Should().NotBeNull();
        published!.MessageId.Should().Be(dto.Id);
        published.ChannelId.Should().Be(channelId);
        published.AuthorId.Should().Be(authorId);
        published.PostedAt.Should().Be(dto.PostedAt);
    }

    [Fact]
    public void MessagePostedEvent_is_plain_data_contract_with_ids_and_date()
    {
        var e = new MessagePostedEvent
        {
            MessageId = Guid.NewGuid(),
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            PostedAt = DateTime.UtcNow
        };

        e.MessageId.Should().NotBe(Guid.Empty);
        e.ChannelId.Should().NotBe(Guid.Empty);
        e.AuthorId.Should().NotBe(Guid.Empty);
    }
}
