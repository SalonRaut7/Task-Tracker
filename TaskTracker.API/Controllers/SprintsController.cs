using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Sprints.Commands.ArchiveSprint;
using TaskTracker.Application.Features.Sprints.Commands.CancelSprint;
using TaskTracker.Application.Features.Sprints.Commands.CompleteSprint;
using TaskTracker.Application.Features.Sprints.Commands.CreateSprint;
using TaskTracker.Application.Features.Sprints.Commands.DeleteSprint;
using TaskTracker.Application.Features.Sprints.Commands.StartSprint;
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

    // ── Queries ──────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SprintDto>>> GetAll(
        [FromQuery] Guid? projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSprintsQuery { ProjectId = projectId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SprintDto>> GetById(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSprintByIdQuery { Id = id }, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SprintDto>> Create(
        [FromBody] CreateSprintCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SprintDto>> Update(
        [FromRoute] Guid id, [FromBody] UpdateSprintCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteSprintCommand { Id = id }, cancellationToken);
        if (!result)
            return NotFound();

        return NoContent();
    }

    //Activates a Planning sprint. Guards: ≥1 task, no active sprint, no date overlap, StartDate not past.
    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<SprintDto>> Start(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartSprintCommand { Id = id }, cancellationToken);
        return Ok(result);
    }

    //Completes an Active sprint. Incomplete tasks are rolled back to the backlog.
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<CompleteSprintResult>> Complete(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteSprintCommand { Id = id }, cancellationToken);
        return Ok(result);
    }

    //Cancels a Planning or Active sprint. All sprint tasks are unlinked to the backlog.
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SprintDto>> Cancel(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSprintCommand { Id = id }, cancellationToken);
        return Ok(result);
    }

    //Archives a Completed or Cancelled sprint for historical reporting.
    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<SprintDto>> Archive(
        [FromRoute] Guid id, [FromBody] ArchiveSprintCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
