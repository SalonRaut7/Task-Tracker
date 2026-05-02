using MediatR;

namespace TaskTracker.Application.Features.Notifications.Commands.EvaluateDueTasks;

public sealed class EvaluateDueTasksCommand : IRequest<Unit>;
