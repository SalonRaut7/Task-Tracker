using AutoMapper;
using MediatR;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Tasks.Notifications;
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
    private readonly IPublisher           _publisher;
    private readonly IUserRepository      _userRepository;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        IMapper mapper,
        IOptions<TaskDateRulesOptions> taskDateRules,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator,
        IPublisher publisher,
        IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
        _taskDateRules = taskDateRules.Value;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
        _publisher = publisher;
        _userRepository = userRepository;
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

        // Capture old state for change detection
        var oldTitle = task.Title;
        var oldDescription = task.Description;
        var oldStatus = (int)task.Status;
        var oldPriority = task.Priority;
        var oldEpicId = task.EpicId;
        var oldSprintId = task.SprintId;
        var oldAssigneeId = task.AssigneeId;
        var oldStartDate = task.StartDate;
        var oldEndDate = task.EndDate;

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

        var dto = _mapper.Map<TaskDto>(task);
        var changedFields = BuildChangedFields(command, oldTitle, oldDescription, oldStatus, oldPriority, oldEpicId, oldSprintId, oldStartDate, oldEndDate);

        // Determine notification event type
        var eventType = "Updated";
        if ((int)command.Status != oldStatus) eventType = "StatusChanged";
        if (assigneeId != oldAssigneeId) eventType = "Reassigned";

        var currentUserId2 = _currentUser.UserId!;
        var actorName = await _userRepository.GetFullNameAsync(currentUserId2, cancellationToken) ?? "Unknown";
        await _publisher.Publish(new TaskChangedNotification
        {
            EventType = eventType,
            Task = dto,
            TaskId = task.Id,
            TaskTitle = task.Title,
            ProjectId = command.ProjectId,
            ActorUserId = currentUserId2,
            ActorName = actorName,
            OldAssigneeId = oldAssigneeId,
            NewAssigneeId = assigneeId,
            OldStatus = oldStatus,
            NewStatus = (int)command.Status,
            ChangedFields = changedFields,
        }, cancellationToken);

        return dto;
    }

    private static IReadOnlyList<TaskFieldChange> BuildChangedFields(
        UpdateTaskCommand command,
        string oldTitle,
        string? oldDescription,
        int oldStatus,
        TaskPriority oldPriority,
        Guid? oldEpicId,
        Guid? oldSprintId,
        DateOnly? oldStartDate,
        DateOnly? oldEndDate)
    {
        var changedFields = new List<TaskFieldChange>();

        if (!string.Equals(oldTitle, command.Title, StringComparison.Ordinal))
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "Title",
                OldValue = oldTitle,
                NewValue = command.Title,
            });
        }

        if (!string.Equals(oldDescription, command.Description, StringComparison.Ordinal))
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "Description",
                OldValue = oldDescription,
                NewValue = command.Description,
            });
        }

        if (oldStatus != (int)command.Status)
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "Status",
                OldValue = FormatStatusLabel(oldStatus),
                NewValue = FormatStatusLabel((int)command.Status),
            });
        }

        if (oldPriority != command.Priority)
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "Priority",
                OldValue = oldPriority.ToString(),
                NewValue = command.Priority.ToString(),
            });
        }

        if (oldEpicId != command.EpicId)
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "Epic",
                OldValue = oldEpicId?.ToString(),
                NewValue = command.EpicId?.ToString(),
            });
        }

        if (oldSprintId != command.SprintId)
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "Sprint",
                OldValue = oldSprintId?.ToString(),
                NewValue = command.SprintId?.ToString(),
            });
        }

        if (oldStartDate != command.StartDate)
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "StartDate",
                OldValue = oldStartDate?.ToString(),
                NewValue = command.StartDate?.ToString(),
            });
        }

        if (oldEndDate != command.EndDate)
        {
            changedFields.Add(new TaskFieldChange
            {
                FieldName = "EndDate",
                OldValue = oldEndDate?.ToString(),
                NewValue = command.EndDate?.ToString(),
            });
        }

        return changedFields;
    }

    private static string FormatStatusLabel(int statusValue)
    {
        return statusValue switch
        {
            (int)Status.NotStarted => "Not Started",
            (int)Status.InProgress => "In Progress",
            (int)Status.Completed => "Completed",
            (int)Status.OnHold => "On Hold",
            (int)Status.Cancelled => "Cancelled",
            _ => statusValue.ToString(),
        };
    }
}