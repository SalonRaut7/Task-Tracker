using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Application.Features.Tasks.Commands.UpdateTask;
using TaskTracker.Application.Features.Tasks.Commands.DeleteTask;
using TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;
using TaskTracker.Application.Features.Tasks.Queries.GetTaskById;
using MediatR;
using AutoMapper;

namespace TaskTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public TasksController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
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
        public async Task<ActionResult<TaskDto>> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var task = await _mediator.Send(new GetTaskByIdQuery { Id = id }, cancellationToken);
            if (task is null) return NotFound();
            return Ok(task);
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskDto dto, CancellationToken cancellationToken)
        {
            // Map DTO -> Command
            var command = _mapper.Map<CreateTaskCommand>(dto);

            // Send command via MediatR
            var task = await _mediator.Send(command, cancellationToken);

            // Return 201 with location
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> Update([FromRoute] int id, [FromBody] UpdateTaskDto dto, CancellationToken cancellationToken)
        {
            var command = _mapper.Map<UpdateTaskCommand>(dto);
            command.Id = id;

            var updatedTask = await _mediator.Send(command, cancellationToken);

            if (updatedTask is null) return NotFound();
            return Ok(updatedTask);
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DeleteTaskCommand { Id = id }, cancellationToken);
            if (!result) return NotFound();
            return NoContent();
        }
    }
    
}
