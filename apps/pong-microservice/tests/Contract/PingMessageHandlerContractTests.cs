using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PongMicroservice.Handlers;
using PongMicroservice.Tests.Fixture;
using Shared.Contracts.Messages;
using Xunit;

namespace PongMicroservice.Tests.Contract;

public class PingMessageHandlerContractTests
{
    private readonly FakeMessageClient _messageClient = new();
    private readonly PingMessageHandler _handler;

    public PingMessageHandlerContractTests()
    {
        _handler = new PingMessageHandler(
            _messageClient,
            NullLogger<PingMessageHandler>.Instance
        );
    }

    private static PingMessage ValidPing(Guid? correlationId = null) => new()
    {
        Message = "ping",
        CorrelationId = correlationId ?? Guid.NewGuid(),
        SentAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handler_CanConsume_MinimumValidContract()
    {
        Func<Task> act = () => _handler.Handle(ValidPing(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handler_PublishesPongMessage_WithEchoedCorrelationId()
    {
        var correlationId = Guid.NewGuid();

        await _handler.Handle(ValidPing(correlationId), CancellationToken.None);

        var published = _messageClient.SinglePublished<PongMessage>();
        published.Should().NotBeNull();
        published.CorrelationId.Should().Be(correlationId,
            because: "PongMessage.CorrelationId must echo PingMessage.CorrelationId");
    }

    [Fact]
    public async Task Handler_PublishesPongMessage_WithNonEmptyMessage()
    {
        await _handler.Handle(ValidPing(), CancellationToken.None);

        var published = _messageClient.SinglePublished<PongMessage>();
        published.Should().NotBeNull();
        published.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handler_PublishesExactlyOnce_PerPingMessage()
    {
        await _handler.Handle(ValidPing(), CancellationToken.None);

        _messageClient.AllPublished<PongMessage>().Should().HaveCount(1);
    }

    [Fact]
    public async Task Handler_IsIdempotent_WhenSameMessageReceivedTwice()
    {
        var ping = ValidPing();

        await _handler.Handle(ping, CancellationToken.None);
        await _handler.Handle(ping, CancellationToken.None);

        _messageClient.AllPublished<PongMessage>().Should().HaveCount(2);
    }
}