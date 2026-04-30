using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Notifications.Commands;
using TaskTracker.Application.Features.Notifications.Queries;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Get the current user's latest notifications.
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetNotificationsQuery { Take = take }, cancellationToken);
        return Ok(result);
    }

    // Mark a single notification as read.
    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult> MarkRead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new MarkNotificationReadCommand { NotificationId = id },
            cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    // Mark all notifications as read for the current user.
    [HttpPatch("read-all")]
    public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return NoContent();
    }
}
