using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Application.Options;

namespace TaskTracker.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskDto?>
{
    private readonly ITaskRepository      _taskRepository;
    private readonly IMapper              _mapper;
    private readonly TaskDateRulesOptions _taskDateRules;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        IMapper mapper,
        IOptions<TaskDateRulesOptions> taskDateRules)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
        _taskDateRules = taskDateRules.Value;
    }

    public async Task<TaskDto?> Handle(
        UpdateTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdForUpdateAsync(
            command.Id, cancellationToken);

        if (task is null) return null;

        task.ApplyUpdate(
            command.Title,
            command.Description,
            command.Status,
            command.Priority,
            command.StartDate,
            command.EndDate,
            command.EndDateExtensionDays,
            _taskDateRules.EffectiveAllowedExtensionDays, // ← clean
            DateTime.UtcNow);

        await _taskRepository.UpdateAsync(task, cancellationToken);

        return _mapper.Map<TaskDto>(task);
    }
}