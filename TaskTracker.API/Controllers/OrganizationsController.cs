using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Organizations.Commands.CreateOrganization;
using TaskTracker.Application.Features.Organizations.Commands.DeleteOrganization;
using TaskTracker.Application.Features.Organizations.Commands.UpdateOrganization;
using TaskTracker.Application.Features.Organizations.Queries.GetOrganizationById;
using TaskTracker.Application.Features.Organizations.Queries.GetOrganizations;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrganizationDto>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationByIdQuery { Id = id }, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateOrganizationCommand
        {
            Name = dto.Name,
            Slug = dto.Slug,
            Description = dto.Description
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OrganizationDto>> Update([FromRoute] Guid id, [FromBody] UpdateOrganizationDto dto, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateOrganizationCommand
        {
            Id = id,
            Name = dto.Name,
            Slug = dto.Slug,
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
        var result = await _mediator.Send(new DeleteOrganizationCommand { Id = id }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
