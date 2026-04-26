using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Application.Options;

namespace TaskTracker.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskDto?>
{
    private readonly ITaskRepository      _taskRepository;
    private readonly IMapper              _mapper;
    private readonly TaskDateRulesOptions _taskDateRules;
    private readonly ICurrentUserService  _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        IMapper mapper,
        IOptions<TaskDateRulesOptions> taskDateRules,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
        _taskDateRules = taskDateRules.Value;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<TaskDto?> Handle(
        UpdateTaskCommand command,
        CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdForUpdateAsync(
            command.Id, cancellationToken);

        if (task is null) return null;

        if (task.ProjectId != command.ProjectId)
        {
            return null;
        }

        if (command.EpicId.HasValue)
        {
            var epicBelongsToProject = await _taskRepository.EpicBelongsToProjectAsync(
                command.EpicId.Value,
                command.ProjectId,
                cancellationToken);

            if (!epicBelongsToProject)
            {
                throw new InvalidOperationException("Epic does not belong to the selected project.");
            }
        }

        if (command.SprintId.HasValue)
        {
            var sprintBelongsToProject = await _taskRepository.SprintBelongsToProjectAsync(
                command.SprintId.Value,
                command.ProjectId,
                cancellationToken);

            if (!sprintBelongsToProject)
            {
                throw new InvalidOperationException("Sprint does not belong to the selected project.");
            }
        }

        var assigneeId = string.IsNullOrWhiteSpace(command.AssigneeId) ? null : command.AssigneeId.Trim();
        var currentUserId = _currentUser.UserId!;

        if (assigneeId != task.AssigneeId)
        {
            bool isSelfAssigning = assigneeId == currentUserId;
            bool isSelfUnassigning = assigneeId == null && task.AssigneeId == currentUserId;

            if (!isSelfAssigning && !isSelfUnassigning)
            {
                var canAssignTasks = await _permissionEvaluator.HasPermissionAsync(
                    currentUserId, AppPermissions.TasksAssign, ScopeType.Project, command.ProjectId, cancellationToken);
                
                if (!canAssignTasks)
                {
                    throw new ForbiddenAccessException($"Missing required permission '{AppPermissions.TasksAssign}'.");
                }
            }

            if (assigneeId != null)
            {
                var canAssignUserToProject = await _taskRepository.CanAssignUserToProjectAsync(
                    assigneeId,
                    command.ProjectId,
                    cancellationToken);

                if (!canAssignUserToProject)
                {
                    throw new InvalidOperationException("Assignee must belong to both the selected project and its organization.");
                }
            }
        }

        task.ApplyUpdate(
            command.Title,
            command.Description,
            command.Status,
            command.Priority,
            command.EpicId,
            command.SprintId,
            assigneeId,
            command.StartDate,
            command.EndDate,
            command.EndDateExtensionDays,
            _taskDateRules.EffectiveAllowedExtensionDays, // ← clean
            DateTime.UtcNow);

        await _taskRepository.UpdateAsync(task, cancellationToken);

        return _mapper.Map<TaskDto>(task);
    }
}