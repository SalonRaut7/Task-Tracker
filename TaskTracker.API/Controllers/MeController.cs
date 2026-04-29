using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Members.Queries.GetMyPermissions;
using TaskTracker.Application.Features.Users.Commands.UpdateCurrentUserProfile;
using TaskTracker.Application.Features.Users.Queries.GetCurrentUserProfile;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the current user's scope-qualified permissions map.
    /// Called by the frontend on login/bootstrap to determine what the user can do where.
    /// </summary>
    [HttpGet("permissions")]
    public async Task<ActionResult<UserPermissionsDto>> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyPermissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("profile")]
    public async Task<ActionResult<CurrentUserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserProfileQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("profile")]
    public async Task<ActionResult<CurrentUserProfileDto>> UpdateProfile(
        [FromBody] UpdateCurrentUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }
}
