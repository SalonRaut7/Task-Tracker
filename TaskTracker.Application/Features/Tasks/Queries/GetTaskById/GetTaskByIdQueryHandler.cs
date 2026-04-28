using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMapper _mapper;
    private readonly IMembershipRepository _membershipRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetTaskByIdQueryHandler(
        ITaskRepository taskRepository,
        IMapper mapper,
        IMembershipRepository membershipRepository,
        UserManager<ApplicationUser> userManager)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
        _membershipRepository = membershipRepository;
        _userManager = userManager;
    }

    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null || task.ProjectId != request.ProjectId)
        {
            return null;
        }

        var dto = _mapper.Map<TaskDto>(task);

        var memberships = await _membershipRepository
            .GetProjectMembershipsAsync(task.ProjectId, cancellationToken);

        var roleByUserId = memberships
            .GroupBy(membership => membership.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(membership => membership.Role).FirstOrDefault(),
                StringComparer.Ordinal);

        var relatedUserIds = new HashSet<string>(StringComparer.Ordinal) { task.ReporterId };
        if (!string.IsNullOrWhiteSpace(task.AssigneeId))
        {
            relatedUserIds.Add(task.AssigneeId!);
        }

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user => relatedUserIds.Contains(user.Id))
            .Select(user => new TaskUserProjection
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                IsArchived = user.IsArchived,
            })
            .ToListAsync(cancellationToken);

        dto.ReporterUser = BuildUserIdentityDto(task.ReporterId, users, roleByUserId);
        dto.AssigneeUser = string.IsNullOrWhiteSpace(task.AssigneeId)
            ? null
            : BuildUserIdentityDto(task.AssigneeId, users, roleByUserId);

        return dto;
    }

    private static TaskUserIdentityDto? BuildUserIdentityDto(
        string? userId,
        IReadOnlyList<TaskUserProjection> users,
        IReadOnlyDictionary<string, string?> roleByUserId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = users.FirstOrDefault(item => string.Equals(item.UserId, userId, StringComparison.Ordinal));
        if (user is null)
        {
            return null;
        }

        roleByUserId.TryGetValue(userId, out var role);

        return new TaskUserIdentityDto
        {
            UserId = userId,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Role = role,
            IsActive = user.IsActive,
            IsArchived = user.IsArchived,
        };
    }

    private sealed class TaskUserProjection
    {
        public string UserId { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public bool IsArchived { get; init; }
    }
}
