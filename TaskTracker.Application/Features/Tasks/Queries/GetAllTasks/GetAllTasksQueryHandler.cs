using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, PagedResultDto<TaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMapper _mapper;

    public GetAllTasksQueryHandler(ITaskRepository taskRepository, IMapper mapper)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<TaskDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        var filteredQuery = ApplyRequestFilters(_taskRepository.Query(), request);
        var orderedQuery = ApplyDefaultOrdering(filteredQuery);

        var skip = request.Skip.GetValueOrDefault();
        var hasTake = request.Take.HasValue && request.Take.Value > 0;
        var take = hasTake ? request.Take!.Value : 0;

        List<TaskItem> tasks;
        int totalCount;

        if (hasTake)
        {
            if (skip == 0)
            {
                var firstPageProbe = await _taskRepository.ToListAsync(
                    orderedQuery.Take(take + 1),
                    cancellationToken);

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
                tasks = await _taskRepository.ToListAsync(
                    orderedQuery.Skip(skip).Take(take),
                    cancellationToken);
                totalCount = await _taskRepository.CountAsync(filteredQuery, cancellationToken);
            }
        }
        else
        {
            tasks = await _taskRepository.ToListAsync(orderedQuery, cancellationToken);
            totalCount = tasks.Count;
        }

        return new PagedResultDto<TaskDto>
        {
            Data = _mapper.Map<List<TaskDto>>(tasks),
            TotalCount = totalCount
        };
    }

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

        // Future filters can be added here with the same pattern:
        // yield return task => task.StartDate >= request.StartDate;
        // yield return task => task.EndDate <= request.EndDate;
        // yield return task => task.AssignedUserId == request.AssignedUserId;
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
