using MessagingMicroservice.Api;
using MessagingMicroservice.Application;
using MessagingMicroservice.Domain;
using MessageClient.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Sagas;

namespace MessagingMicroservice.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User")]
public class MessagesController : ControllerBase
{
    private readonly MessagesService _messagesService;
    private readonly IMessageClient _messageClient;

    public MessagesController(MessagesService messagesService, IMessageClient messageClient)
    {
        _messagesService = messagesService;
        _messageClient = messageClient;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await _messagesService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _messagesService.GetByIdAsync(id, cancellationToken);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("async")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateViaSaga(
        [FromBody] CreateMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var sagaId = Guid.NewGuid();
        await _messageClient.PublishAsync(
            new InitiateMessagePost
            {
                SagaId = sagaId,
                ChannelId = request.ChannelId,
                AuthorId = request.AuthorId,
                Content = request.Content
            },
            cancellationToken);

        return Accepted(new { sagaId });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var dto = await _messagesService.CreateAsync(
                request.ChannelId,
                request.AuthorId,
                request.Content,
                cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var dto = await _messagesService.UpdateAsync(id, request.Content, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var removed = await _messagesService.DeleteAsync(id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
