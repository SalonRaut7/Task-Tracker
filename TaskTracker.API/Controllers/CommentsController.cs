using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Comments.Commands.CreateComment;
using TaskTracker.Application.Features.Comments.Commands.DeleteComment;
using TaskTracker.Application.Features.Comments.Commands.UpdateComment;
using TaskTracker.Application.Features.Comments.Queries.GetCommentById;
using TaskTracker.Application.Features.Comments.Queries.GetComments;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CommentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetAll([FromQuery] int? taskId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCommentsQuery { TaskId = taskId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommentDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCommentByIdQuery { Id = id }, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create([FromBody] CreateCommentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommentDto>> Update([FromRoute] Guid id, [FromBody] UpdateCommentCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteCommentCommand { Id = id }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
