extern alias SampleMicroservice;
extern alias PongMicroservice;

using System.Net;
using FluentAssertions;
using MessageClient.Types;
using Shared.Contracts.Messages;
using E2E.Tests.Fixture;
using Xunit;

namespace E2E.Tests;

public class PingPongE2ETests : IClassFixture<E2EFixture>
{
    private readonly E2EFixture _fixture;

    public PingPongE2ETests(E2EFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Post_ToPingPong_ResultsIn_PongMessage_BeingPublished()
    {
        // Use PongMessageCapture with a unique subscription ID per instance
        var capture = new PongMessageCapture(_fixture.MessageClient);

        var response = await _fixture.SampleMicroserviceClient.PostAsync("/pingpong", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pong = await capture.WaitAsync(TimeSpan.FromSeconds(10));
        pong.Message.Should().Be("pong");
        pong.CorrelationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CorrelationId_IsPreserved_AcrossServices()
    {
        // Give each subscription a unique ID so this test's queues
        // don't compete with other tests or leftover subscriptions
        var pingId = new MessageSubscription($"e2e-ping-{Guid.NewGuid()}");
        var pongId = new MessageSubscription($"e2e-pong-{Guid.NewGuid()}");

        var pingTcs = new TaskCompletionSource<PingMessage>();
        var pongTcs = new TaskCompletionSource<PongMessage>();

        await _fixture.MessageClient.SubscribeAsync<PingMessage>(pingId, msg =>
            pingTcs.TrySetResult(msg));

        await _fixture.MessageClient.SubscribeAsync<PongMessage>(pongId, msg =>
            pongTcs.TrySetResult(msg));

        // Trigger the full chain
        await _fixture.SampleMicroserviceClient.PostAsync("/pingpong", null);

        // Wait for both with a shared deadline
        var timeout = Task.Delay(TimeSpan.FromSeconds(10));

        var pingDone = await Task.WhenAny(pingTcs.Task, timeout);
        pingDone.Should().Be(pingTcs.Task, because: "PingMessage should arrive within 10s");

        var pongDone = await Task.WhenAny(pongTcs.Task, timeout);
        pongDone.Should().Be(pongTcs.Task, because: "PongMessage should arrive within 10s");

        // The contract assertion
        pongTcs.Task.Result.CorrelationId.Should().Be(pingTcs.Task.Result.CorrelationId,
            because: "PongService must echo the CorrelationId from PingMessage");
    }
}