using AutoMapper;
using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, List<TaskDto>>
{
    private readonly ITaskRepository _taskRepository;
    private readonly IMapper _mapper;

    public GetAllTasksQueryHandler(ITaskRepository taskRepository, IMapper mapper)
    {
        _taskRepository = taskRepository;
        _mapper = mapper;
    }

    public async Task<List<TaskDto>> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await _taskRepository.ListAsync(request.TitleContains, request.Status,request.Priority, cancellationToken);

        return _mapper.Map<List<TaskDto>>(tasks);
    }
}