using FluentAssertions;
using SampleMicroservice.Tests.Fixture;
using Shared.Contracts.Messages;
using Xunit;

namespace SampleMicroservice.Tests.Contract;

public class PingMessageContractTests : IClassFixture<SampleMicroserviceFactory>
{
    private readonly HttpClient _client;
    private readonly SampleMicroserviceFactory _factory;

    public PingMessageContractTests(SampleMicroserviceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PublishedPingMessage_MeetsContract_MessageField()
    {
        await using var capture = new MessageCapture<PingMessage>(_factory.MessageClient);

        await _client.PostAsync("/pingpong", null);
        var message = await capture.WaitForMessageAsync();

        message.Message.Should().NotBeNullOrEmpty(
            because: "PongService reads Message in its log statement");
    }

    [Fact]
    public async Task PublishedPingMessage_MeetsContract_CorrelationIdField()
    {
        await using var capture = new MessageCapture<PingMessage>(_factory.MessageClient);

        await _client.PostAsync("/pingpong", null);
        var message = await capture.WaitForMessageAsync();

        message.CorrelationId.Should().NotBe(Guid.Empty,
            because: "PongService echoes CorrelationId into its PongMessage reply");
    }

    [Fact]
    public async Task PublishedPingMessage_MeetsContract_SentAtField()
    {
        var before = DateTime.UtcNow;

        await using var capture = new MessageCapture<PingMessage>(_factory.MessageClient);
        await _client.PostAsync("/pingpong", null);
        var message = await capture.WaitForMessageAsync();

        var after = DateTime.UtcNow;

        message.SentAt.Should().BeAfter(before.AddSeconds(-1))
            .And.BeBefore(after.AddSeconds(1),
            because: "SentAt must reflect the actual publish time");
    }

    [Fact]
    public async Task PublishedPingMessage_MeetsContract_FullShape()
    {
        await using var capture = new MessageCapture<PingMessage>(_factory.MessageClient);

        await _client.PostAsync("/pingpong", null);
        var message = await capture.WaitForMessageAsync();

        using var _ = new FluentAssertions.Execution.AssertionScope();
        message.Message.Should().NotBeNullOrEmpty();
        message.CorrelationId.Should().NotBe(Guid.Empty);
        message.SentAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
    }
}