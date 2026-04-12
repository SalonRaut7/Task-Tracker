using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Application.Options;

namespace TaskTracker.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMapper _mapper;
    private readonly TaskDateRulesOptions _taskDateRules;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        IMapper mapper,
        IOptions<TaskDateRulesOptions> taskDateRules)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
        _taskDateRules = taskDateRules.Value;
    }

    public async Task<TaskDto> Handle(
        CreateTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = TaskItem.Create(
            command.Title,
            command.Description,
            command.Status,
            command.Priority,
            command.StartDate,
            command.EndDate,
            _taskDateRules.EffectiveSprintDays,   // no fallback logic here
            DateTime.UtcNow);

        await _taskRepository.AddAsync(task, cancellationToken);

        return _mapper.Map<TaskDto>(task);
    }
}