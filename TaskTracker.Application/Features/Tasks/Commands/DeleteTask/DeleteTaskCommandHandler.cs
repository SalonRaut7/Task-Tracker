using MediatR;
using TaskTracker.Application.Features.Tasks.Notifications;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, bool>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IPublisher _publisher;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _userRepository;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        IPublisher publisher,
        ICurrentUserService currentUser,
        IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _publisher = publisher;
        _currentUser = currentUser;
        _userRepository = userRepository;
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

        await _taskRepository.DeleteAsync(task, cancellationToken);

        // Publish real-time notification
        var currentUserId = _currentUser.UserId!;
        var actorName = await _userRepository.GetFullNameAsync(currentUserId, cancellationToken) ?? "Unknown";
        await _publisher.Publish(new TaskChangedNotification
        {
            EventType = "Deleted",
            Task = null,
            TaskId = taskId,
            TaskTitle = taskTitle,
            ProjectId = projectId,
            ActorUserId = currentUserId,
            ActorName = actorName,
        }, cancellationToken);

        return true;
    }
}