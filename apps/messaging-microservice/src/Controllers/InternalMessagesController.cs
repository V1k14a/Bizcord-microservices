using MessagingMicroservice.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MessagingMicroservice.Controllers;

/// <summary>
/// East/west endpoints for other microservices (JWT role <c>Service</c>). Not intended for end-user clients.
/// </summary>
[ApiController]
[Route("api/internal/messages")]
[Authorize(Roles = "Service")]
public class InternalMessagesController : ControllerBase
{
    private readonly MessagesService _messagesService;

    public InternalMessagesController(MessagesService messagesService) =>
        _messagesService = messagesService;

    [HttpGet("channel/{channelId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByChannel(Guid channelId, CancellationToken cancellationToken)
    {
        var items = await _messagesService.GetByChannelAsync(channelId, cancellationToken);
        return Ok(items);
    }
}
