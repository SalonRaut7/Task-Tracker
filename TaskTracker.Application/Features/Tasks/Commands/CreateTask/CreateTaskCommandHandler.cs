using AutoMapper;
using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public CreateTaskCommandHandler(
        ITaskRepository taskRepository,
        IMapper mapper,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<TaskDto> Handle(
        CreateTaskCommand command,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var currentUserId = _currentUser.UserId!;

        var projectExists = await _taskRepository.ProjectExistsAsync(command.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new KeyNotFoundException($"Project '{command.ProjectId}' was not found.");
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
        if (!string.IsNullOrWhiteSpace(assigneeId) && assigneeId != currentUserId)
        {
            var canAssignTasks = await _permissionEvaluator.HasPermissionAsync(
                currentUserId, AppPermissions.TasksAssign, ScopeType.Project, command.ProjectId, cancellationToken);
            if (!canAssignTasks)
            {
                throw new ForbiddenAccessException($"Missing required permission '{AppPermissions.TasksAssign}'.");
            }

            var canAssignUserToProject = await _taskRepository.CanAssignUserToProjectAsync(
                assigneeId,
                command.ProjectId,
                cancellationToken);

            if (!canAssignUserToProject)
            {
                throw new InvalidOperationException("Assignee must belong to both the selected project and its organization.");
            }
        }

        var task = TaskItem.Create(
            command.ProjectId,
            command.EpicId,
            command.SprintId,
            assigneeId,
            currentUserId,
            command.Title,
            command.Description,
            command.Status,
            command.Priority,
            command.StartDate,
            command.EndDate,
            DateTime.UtcNow);

        await _taskRepository.AddAsync(task, cancellationToken);

        return _mapper.Map<TaskDto>(task);
    }
}