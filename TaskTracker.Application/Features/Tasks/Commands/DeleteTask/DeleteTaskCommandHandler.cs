using MediatR;
using TaskTracker.Application.Constants;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Events;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ICacheService _cache;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        ICurrentUserService currentUser,
        ICacheService cache)
    {
        _taskRepository = taskRepository;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<bool> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (task is null)
        {
            return false;
        }

        if (task.ProjectId != request.ProjectId)
        {
            return false;
        }

        var taskTitle = task.Title;
        var taskId = task.Id;
        var projectId = task.ProjectId;

        var currentUserId = _currentUser.UserId!;

        task.RaiseChangedEvent(new TaskChangedDomainEvent
        {
            EventType = "Deleted",
            Task = null,
            TaskEntity = task,
            TaskId = taskId,
            TaskTitle = taskTitle,
            ProjectId = projectId,
            ActorUserId = currentUserId,
        });

        await _taskRepository.DeleteAsync(task, cancellationToken);

        // Remove the task→project scope mapping from cache.
        // Without this, a new task created with the same int ID (unlikely but possible)
        // could pick up the stale projectId entry.
        _cache.Remove(CacheKeys.TaskProject(taskId));

        return true;
    }
}
