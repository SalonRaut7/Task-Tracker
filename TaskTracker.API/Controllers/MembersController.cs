using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Members.Commands.RemoveMember;
using TaskTracker.Application.Features.Members.Commands.UpdateMemberRole;
using TaskTracker.Application.Features.Members.Queries.GetMembersByScope;
using TaskTracker.Domain.Enums;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly IMediator _mediator;

    public MembersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>List members and pending invitations for a scope.</summary>
    [HttpGet]
    public async Task<ActionResult<ScopeMembersDto>> GetByScope(
        [FromQuery] ScopeType scopeType, [FromQuery] Guid scopeId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMembersByScopeQuery
        {
            ScopeType = scopeType,
            ScopeId = scopeId
        }, cancellationToken);

        return Ok(result);
    }

    /// <summary>Update a member's scoped role.</summary>
    [HttpPut("role")]
    public async Task<ActionResult<MemberDto>> UpdateRole(
        [FromBody] UpdateMemberRoleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>Remove a member from a scope. Cascade removes project memberships for org removal.</summary>
    [HttpDelete]
    public async Task<ActionResult> Remove(
        [FromQuery] ScopeType scopeType, [FromQuery] Guid scopeId, [FromQuery] string userId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveMemberCommand
        {
            ScopeType = scopeType,
            ScopeId = scopeId,
            UserId = userId
        }, cancellationToken);

        return NoContent();
    }
}
