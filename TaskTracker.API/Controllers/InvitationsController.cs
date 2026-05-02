using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Invitations.Commands.AcceptInvitation;
using TaskTracker.Application.Features.Invitations.Commands.CreateInvitation;
using TaskTracker.Application.Features.Invitations.Commands.ResendInvitation;
using TaskTracker.Application.Features.Invitations.Commands.RevokeInvitation;
using TaskTracker.Application.Features.Invitations.Queries.GetInvitationsByScope;
using TaskTracker.Domain.Enums;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Create a new invitation to a scope.</summary>
    [HttpPost]
    public async Task<ActionResult<InvitationDto>> Create(
        [FromBody] CreateInvitationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(null, result);
    }

    /// <summary>Resend a pending invitation (regenerates token and extends expiry).</summary>
    [HttpPost("{id:guid}/resend")]
    public async Task<ActionResult<InvitationDto>> Resend(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResendInvitationCommand { InvitationId = id }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Revoke a pending invitation.</summary>
    [HttpPost("{id:guid}/revoke")]
    public async Task<ActionResult> Revoke(
        [FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RevokeInvitationCommand { InvitationId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Accept an invitation using the raw token.</summary>
    [HttpPost("accept")]
    public async Task<ActionResult<AcceptInvitationResult>> Accept(
        [FromBody] AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invitation could not be accepted.",
                Detail = result.Message,
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(result);
    }

    /// <summary>List invitations for a scope.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvitationDto>>> GetByScope(
        [FromQuery] ScopeType scopeType, [FromQuery] Guid scopeId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInvitationsByScopeQuery
        {
            ScopeType = scopeType,
            ScopeId = scopeId
        }, cancellationToken);

        return Ok(result);
    }
}
