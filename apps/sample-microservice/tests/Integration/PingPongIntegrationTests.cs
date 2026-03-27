using System.Net;
using FluentAssertions;
using SampleMicroservice.Tests.Fixture;
using Shared.Contracts.Messages;
using Xunit;

namespace SampleMicroservice.Tests.Integration;

public class PingPongIntegrationTests : IClassFixture<SampleMicroserviceFactory>
{
    private readonly HttpClient _client;
    private readonly SampleMicroserviceFactory _factory;

    public PingPongIntegrationTests(SampleMicroserviceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturn_Pong()
    {
        var response = await _client.GetAsync("/pingpong");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("pong");
    }

    [Fact]
    public async Task Post_ShouldReturn200_AndPublish_PingMessage()
    {
        // Arrange — subscribe BEFORE posting, or you'll miss the message
        await using var capture = new MessageCapture<PingMessage>(_factory.MessageClient);

        // Act
        var response = await _client.PostAsync("/pingpong", null);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert the message was actually published to RabbitMQ
        // WaitForMessageAsync throws TimeoutException if nothing arrives
        var message = await capture.WaitForMessageAsync(TimeSpan.FromSeconds(5));
        message.Should().NotBeNull();
        message.Message.Should().Be("pong");
    }

    [Fact]
    public async Task Post_MultipleRequests_EachPublishes_SeparateMessage()
    {
        await using var capture = new MessageCapture<PingMessage>(_factory.MessageClient);

        await _client.PostAsync("/pingpong", null);
        await _client.PostAsync("/pingpong", null);
        await _client.PostAsync("/pingpong", null);

        // Each POST should produce exactly one message
        var msg1 = await capture.WaitForMessageAsync();
        var msg2 = await capture.WaitForMessageAsync();
        var msg3 = await capture.WaitForMessageAsync();

        // All messages should have the expected content
        new[] { msg1, msg2, msg3 }
            .Should().AllSatisfy(m => m.Message.Should().Be("pong"));
    }
}