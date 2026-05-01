using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Users.Commands.ArchiveUser;
using TaskTracker.Application.Features.Users.Commands.PermanentlyDeleteUser;
using TaskTracker.Application.Features.Users.Commands.RestoreUser;
using TaskTracker.Application.Features.Users.Queries.GetArchivedUsers;
using TaskTracker.Application.Features.Users.Queries.GetUserDetails;
using TaskTracker.Application.Features.Users.Queries.GetUsers;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<UserSummaryDto>>> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(query, cancellationToken);
        return Ok(users);
    }

    [HttpGet("archive")]
    public async Task<ActionResult<PagedResultDto<UserSummaryDto>>> GetArchivedUsers([FromQuery] GetArchivedUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await _mediator.Send(query, cancellationToken);
        return Ok(users);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDetailsDto>> GetUserDetails([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var user = await _mediator.Send(new GetUserDetailsQuery { UserId = userId }, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost("{userId}/archive")]
    public async Task<ActionResult> Archive(
        [FromRoute] string userId,
        [FromBody] ArchiveUserDto? dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ArchiveUserCommand
            {
                UserId = userId,
                Reason = dto?.Reason,
            },
            cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("{userId}/restore")]
    public async Task<ActionResult> Restore([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RestoreUserCommand { UserId = userId }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{userId}/permanent")]
    public async Task<ActionResult> PermanentlyDelete([FromRoute] string userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PermanentlyDeleteUserCommand { UserId = userId }, cancellationToken);
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
