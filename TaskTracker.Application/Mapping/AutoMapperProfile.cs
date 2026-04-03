using AutoMapper;
using TaskTracker.Domain.Entities;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Application.Features.Tasks.Commands.UpdateTask;

namespace TaskTracker.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // API DTOs -> commands
            CreateMap<CreateTaskDto, CreateTaskCommand>();
            CreateMap<UpdateTaskDto, UpdateTaskCommand>();

            // Commands -> Entity
            CreateMap<CreateTaskCommand, TaskItem>();
            CreateMap<UpdateTaskCommand, TaskItem>();

            // Entity -> DTO
            CreateMap<TaskItem, TaskDto>();
        }
    }
}