using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, PagedResultDto<TaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public GetAllTasksQueryHandler(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<PagedResultDto<TaskDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        var query = _taskRepository.Query();

        if (!IsSuperAdmin())
        {
            var userId = _currentUser.UserId!;

            if (request.ProjectId.HasValue)
            {
                var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value, cancellationToken);
                if (project is null)
                {
                    throw new ForbiddenAccessException(
                        $"No access to Project resource '{request.ProjectId.Value}'.");
                }

                var orgIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId, cancellationToken);
                if (!orgIds.Contains(project.OrganizationId))
                {
                    throw new ForbiddenAccessException(
                        $"No access to Project resource '{request.ProjectId.Value}'.");
                }
            }
            else
            {
                var organizationIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId, cancellationToken);
                var projectIds = await _membershipRepository.GetUserProjectIdsAsync(userId, cancellationToken);

                if (organizationIds.Count == 0 || projectIds.Count == 0)
                {
                    return new PagedResultDto<TaskDto>
                    {
                        Data = [],
                        TotalCount = 0
                    };
                }

                query = query.Where(task =>
                    projectIds.Contains(task.ProjectId)
                    && organizationIds.Contains(task.Project.OrganizationId));
            }
        }

        var filteredQuery = ApplyRequestFilters(query, request);
        var orderedQuery = ApplyDefaultOrdering(filteredQuery);

        var skip = request.Skip.GetValueOrDefault();
        var hasTake = request.Take.HasValue && request.Take.Value > 0;
        var take = hasTake ? request.Take!.Value : 0;
        var projectedQuery = orderedQuery.Select(TaskDtoMapper.Projection());

        List<TaskDto> tasks;
        int totalCount;

        if (hasTake)
        {
            if (skip == 0)
            {
                var firstPageProbe = await projectedQuery
                    .Take(take + 1)
                    .ToListAsync(cancellationToken);

                if (firstPageProbe.Count <= take)
                {
                    tasks = firstPageProbe;
                    totalCount = firstPageProbe.Count;
                }
                else
                {
                    tasks = firstPageProbe.Take(take).ToList();
                    totalCount = await _taskRepository.CountAsync(filteredQuery, cancellationToken);
                }
            }
            else
            {
                tasks = await projectedQuery
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(cancellationToken);
                totalCount = await _taskRepository.CountAsync(filteredQuery, cancellationToken);
            }
        }
        else
        {
            tasks = await projectedQuery.ToListAsync(cancellationToken);
            totalCount = tasks.Count;
        }

        return new PagedResultDto<TaskDto>
        {
            Data = tasks,
            TotalCount = totalCount
        };
    }

    private bool IsSuperAdmin() => _currentUser.IsSuperAdmin;

    private static IQueryable<TaskItem> ApplyRequestFilters(IQueryable<TaskItem> query, GetAllTasksQuery request)
    {
        foreach (var filter in BuildFilters(request))
        {
            query = query.Where(filter);
        }

        return query;
    }

    private static IEnumerable<Expression<Func<TaskItem, bool>>> BuildFilters(GetAllTasksQuery request)
    {
        if (request.ProjectId.HasValue)
        {
            var projectId = request.ProjectId.Value;
            yield return task => task.ProjectId == projectId;
        }

        if (request.EpicId.HasValue)
        {
            var epicId = request.EpicId.Value;
            yield return task => task.EpicId == epicId;
        }

        if (request.SprintId.HasValue)
        {
            var sprintId = request.SprintId.Value;
            yield return task => task.SprintId == sprintId;
        }

        if (!string.IsNullOrWhiteSpace(request.AssigneeId))
        {
            var assigneeId = request.AssigneeId.Trim();
            yield return task => task.AssigneeId == assigneeId;
        }

        if (!string.IsNullOrWhiteSpace(request.ReporterId))
        {
            var reporterId = request.ReporterId.Trim();
            yield return task => task.ReporterId == reporterId;
        }

        if (!string.IsNullOrWhiteSpace(request.TitleContains))
        {
            var likePattern = BuildContainsPattern(request.TitleContains);
            yield return task => EF.Functions.ILike(task.Title, likePattern);
        }

        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            yield return task => task.Status == status;
        }

        if (request.Priority.HasValue)
        {
            var priority = request.Priority.Value;
            yield return task => task.Priority == priority;
        }
    }

    private static IOrderedQueryable<TaskItem> ApplyDefaultOrdering(IQueryable<TaskItem> query)
    {
        return query
            .OrderByDescending(task => task.CreatedAt)
            .ThenByDescending(task => task.Id);
    }

    private static string BuildContainsPattern(string input)
    {
        var escapedValue = input.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return $"%{escapedValue}%";
    }

}
