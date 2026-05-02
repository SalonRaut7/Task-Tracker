using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Epics.Commands.CreateEpic;
using TaskTracker.Application.Features.Epics.Commands.DeleteEpic;
using TaskTracker.Application.Features.Epics.Commands.UpdateEpic;
using TaskTracker.Application.Features.Epics.Queries.GetEpicById;
using TaskTracker.Application.Features.Epics.Queries.GetEpics;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EpicsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EpicsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EpicDto>>> GetAll([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEpicsQuery { ProjectId = projectId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EpicDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEpicByIdQuery { Id = id }, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EpicDto>> Create([FromBody] CreateEpicCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EpicDto>> Update([FromRoute] Guid id, [FromBody] UpdateEpicCommand command, CancellationToken cancellationToken)
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
        var result = await _mediator.Send(new DeleteEpicCommand { Id = id }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
