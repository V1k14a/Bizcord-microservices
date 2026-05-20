using MessageClient.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Messages;
    
namespace src.Controllers;

[ApiController]
[Route("[controller]")]
public class PingPongController : ControllerBase
{
    private readonly IMessageClient _messageClient;

    public PingPongController(IMessageClient messageClient)
    {
        _messageClient = messageClient;
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("pong");
    }
    
    [HttpPost]
    public IActionResult Post()
    {
        PingMessage pingMessage = new PingMessage {  Message = "pong" };
        _messageClient.PublishAsync<PingMessage>(pingMessage);
        return Ok("pong");
    }
}
