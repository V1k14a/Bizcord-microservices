using MessageClient.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Messages;

namespace PongMicroservice.Controllers;

[ApiController]
[Route("[controller]")]
public class PongController : ControllerBase
{
    private readonly IMessageClient _messageClient;

    public PongController(IMessageClient messageClient)
    {
        _messageClient = messageClient;
    }

    // Health check — confirms the service is up
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("pong service is running");
    }

    // Manually publish a PongMessage — useful for testing the consumer side
    // without having to trigger a full ping → pong flow
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        var pong = new PongMessage
        {
            Message = "pong",
            CorrelationId = Guid.NewGuid(),
            RepliedAt = DateTime.UtcNow
        };

        await _messageClient.PublishAsync(pong);

        return Ok(pong);
    }
}