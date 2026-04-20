using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Sprints.Commands.CreateSprint;
using TaskTracker.Application.Features.Sprints.Commands.DeleteSprint;
using TaskTracker.Application.Features.Sprints.Commands.UpdateSprint;
using TaskTracker.Application.Features.Sprints.Queries.GetSprintById;
using TaskTracker.Application.Features.Sprints.Queries.GetSprints;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SprintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SprintDto>>> GetAll([FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSprintsQuery { ProjectId = projectId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SprintDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSprintByIdQuery { Id = id }, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SprintDto>> Create([FromBody] CreateSprintDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateSprintCommand
        {
            ProjectId = dto.ProjectId,
            Name = dto.Name,
            Goal = dto.Goal,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SprintDto>> Update([FromRoute] Guid id, [FromBody] UpdateSprintDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateSprintCommand
        {
            Id = id,
            Name = dto.Name,
            Goal = dto.Goal,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status
        }, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteSprintCommand { Id = id }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
