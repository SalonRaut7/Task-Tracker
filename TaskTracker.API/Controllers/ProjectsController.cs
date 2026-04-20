using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Projects.Commands.CreateProject;
using TaskTracker.Application.Features.Projects.Commands.DeleteProject;
using TaskTracker.Application.Features.Projects.Commands.UpdateProject;
using TaskTracker.Application.Features.Projects.Queries.GetProjectById;
using TaskTracker.Application.Features.Projects.Queries.GetProjects;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectDto>>> GetAll([FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectsQuery { OrganizationId = organizationId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery { Id = id }, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateProjectCommand
        {
            OrganizationId = dto.OrganizationId,
            Name = dto.Name,
            Key = dto.Key,
            Description = dto.Description
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update([FromRoute] Guid id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateProjectCommand
        {
            Id = id,
            Name = dto.Name,
            Key = dto.Key,
            Description = dto.Description
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
        var result = await _mediator.Send(new DeleteProjectCommand { Id = id }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
