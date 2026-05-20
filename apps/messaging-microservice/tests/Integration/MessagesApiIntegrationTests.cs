using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using MessagingMicroservice.Api;
using MessagingMicroservice.Tests.Fixture;
using Shared.Contracts.Events;
using Shared.Contracts.Messages;
using Shared.Contracts.Sagas;
using Xunit;

namespace MessagingMicroservice.Tests.Integration;

public class MessagesApiIntegrationTests : IClassFixture<MessagingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly RecordingMessageClient _recording;

    public MessagesApiIntegrationTests(MessagingWebApplicationFactory factory)
    {
        _recording = factory.Recording;
        _recording.Clear();
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestTokens.User());
    }

    [Fact]
    public async Task Post_creates_message_returns_201_and_publishes_event()
    {
        var channelId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var request = new CreateMessageRequest
        {
            ChannelId = channelId,
            AuthorId = authorId,
            Content = "integration"
        };

        var response = await _client.PostAsJsonAsync("/api/messages", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<MessageDto>();
        dto.Should().NotBeNull();
        dto!.Content.Should().Be("integration");
        dto.ChannelId.Should().Be(channelId);

        var evt = _recording.SinglePublished<MessagePostedEvent>();
        evt.Should().NotBeNull();
        evt!.MessageId.Should().Be(dto.Id);
    }

    [Fact]
    public async Task GetById_after_create_returns_message()
    {
        var create = new CreateMessageRequest
        {
            ChannelId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Content = "read me"
        };
        var created = await _client.PostAsJsonAsync("/api/messages", create);
        var dto = await created.Content.ReadFromJsonAsync<MessageDto>();

        var get = await _client.GetAsync($"/api/messages/{dto!.Id}");

        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var roundTrip = await get.Content.ReadFromJsonAsync<MessageDto>();
        roundTrip!.Id.Should().Be(dto.Id);
        roundTrip.Content.Should().Be("read me");
    }

    [Fact]
    public async Task Put_updates_message()
    {
        var created = await _client.PostAsJsonAsync(
            "/api/messages",
            new CreateMessageRequest
            {
                ChannelId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Content = "v1"
            });
        var dto = await created.Content.ReadFromJsonAsync<MessageDto>();

        var put = await _client.PutAsJsonAsync(
            $"/api/messages/{dto!.Id}",
            new UpdateMessageRequest { Content = "v2" });

        put.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await put.Content.ReadFromJsonAsync<MessageDto>();
        updated!.Content.Should().Be("v2");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_returns_204_and_get_404()
    {
        var created = await _client.PostAsJsonAsync(
            "/api/messages",
            new CreateMessageRequest
            {
                ChannelId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Content = "delete me"
            });
        var dto = await created.Content.ReadFromJsonAsync<MessageDto>();

        var del = await _client.DeleteAsync($"/api/messages/{dto!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetAsync($"/api/messages/{dto.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_invalid_body_returns_400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/messages",
            new CreateMessageRequest
            {
                ChannelId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Content = ""
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_async_returns_202_and_publishes_InitiateMessagePost()
    {
        var channelId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync(
            "/api/messages/async",
            new CreateMessageRequest
            {
                ChannelId = channelId,
                AuthorId = Guid.NewGuid(),
                Content = "saga"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<SagaAcceptedResponse>();
        body.Should().NotBeNull();
        body!.SagaId.Should().NotBeEmpty();

        var initiated = _recording.SinglePublished<InitiateMessagePost>();
        initiated.Should().NotBeNull();
        initiated!.SagaId.Should().Be(body.SagaId);
        initiated.ChannelId.Should().Be(channelId);
        initiated.Content.Should().Be("saga");
    }

    [Fact]
    public async Task Internal_channel_list_requires_service_role()
    {
        var channelId = Guid.NewGuid();
        await _client.PostAsJsonAsync(
            "/api/messages",
            new CreateMessageRequest
            {
                ChannelId = channelId,
                AuthorId = Guid.NewGuid(),
                Content = "internal gate"
            });

        var forbidden = await _client.GetAsync($"/api/internal/messages/channel/{channelId}");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        try
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTestTokens.Service());
            var ok = await _client.GetAsync($"/api/internal/messages/channel/{channelId}");
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await ok.Content.ReadFromJsonAsync<List<MessageDto>>();
            list.Should().NotBeNull();
            list!.Should().ContainSingle(m => m.Content == "internal gate");
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", JwtTestTokens.User());
        }
    }

    private sealed record SagaAcceptedResponse(Guid SagaId);
}
