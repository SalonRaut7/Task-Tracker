using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Application.Features.Tasks.Commands.UpdateTask;
using TaskTracker.Application.Features.Tasks.Commands.DeleteTask;
using TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;
using TaskTracker.Application.Features.Tasks.Queries.GetTaskById;
using MediatR;

namespace TaskTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<TaskDto>>> GetAll([FromQuery] GetAllTasksQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetById([FromRoute] int id, [FromQuery] Guid projectId, CancellationToken cancellationToken)
        {
            var task = await _mediator.Send(new GetTaskByIdQuery { Id = id, ProjectId = projectId }, cancellationToken);
            if (task is null) return NotFound();
            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskCommand command, CancellationToken cancellationToken)
        {
            var task = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = task.Id, projectId = task.ProjectId }, task);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> Update([FromRoute] int id, [FromQuery] Guid projectId, [FromBody] UpdateTaskCommand command, CancellationToken cancellationToken)
        {
            command.Id = id;
            command.ProjectId = projectId;

            var updatedTask = await _mediator.Send(command, cancellationToken);

            if (updatedTask is null) return NotFound();
            return Ok(updatedTask);
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] int id, [FromQuery] Guid projectId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteTaskCommand { Id = id, ProjectId = projectId }, cancellationToken);
            if (!result) return NotFound();
            return NoContent();
        }
    }
    
}
