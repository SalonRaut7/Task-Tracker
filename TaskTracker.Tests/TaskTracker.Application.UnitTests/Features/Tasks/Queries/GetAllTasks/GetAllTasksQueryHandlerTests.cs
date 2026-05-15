using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.Moq;
using Moq;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using Xunit;

namespace TaskTracker.Tests.TaskTracker.Application.UnitTests.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQueryHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<IMembershipRepository> _membershipRepositoryMock;
    private readonly GetAllTasksQueryHandler _handler;

    private readonly string _userId = "user-123";

    public GetAllTasksQueryHandlerTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _currentUserMock = new Mock<ICurrentUserService>();
        _membershipRepositoryMock = new Mock<IMembershipRepository>();

        _currentUserMock.Setup(c => c.UserId).Returns(_userId);
        _currentUserMock.Setup(c => c.IsSuperAdmin).Returns(false);

        _handler = new GetAllTasksQueryHandler(
            _taskRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _currentUserMock.Object,
            _membershipRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult_WithCorrectTakeAndSkip()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var query = new GetAllTasksQuery { Skip = 2, Take = 2 };

        // Ensure user has access to these projects
        _membershipRepositoryMock.Setup(r => r.GetUserOrganizationIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { orgId }.ToList());
        _membershipRepositoryMock.Setup(r => r.GetUserProjectIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { projectId }.ToList());

        var project = new Project { Id = projectId, OrganizationId = orgId };

        // Generate 5 mock tasks
        var tasks = Enumerable.Range(1, 5).Select(i => 
        {
            var t = TaskItem.Create(projectId, null, null, null, "reporter-1", $"Task {i}", null, default, default, null, null, DateTime.UtcNow);
            typeof(TaskItem).GetProperty("Id")!.SetValue(t, i);
            typeof(TaskItem).GetProperty("Project")!.SetValue(t, project);
            typeof(TaskItem).GetProperty("CreatedAt")!.SetValue(t, DateTime.UtcNow.AddDays(-i)); // Order by creation desc
            return t;
        }).ToList();

        var mockDbSet = tasks.AsQueryable().BuildMock();
        
        // Setup repository
        _taskRepositoryMock.Setup(r => r.Query()).Returns(mockDbSet);
        _taskRepositoryMock.Setup(r => r.CountAsync(It.IsAny<IQueryable<TaskItem>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks.Count);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount); // Total items in db
        Assert.Equal(2, result.Data.Count); // Only "Take" items mapped
        // Skip 2 means we should get Tasks 3 and 4 based on our dates
        Assert.Equal("Task 3", result.Data[0].Title);
        Assert.Equal("Task 4", result.Data[1].Title);
    }


    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenUserHasNoOrganizationsOrProjects()
    {
        // Arrange
        var query = new GetAllTasksQuery(); // No specific project requested

        // User belongs to nothing
        _membershipRepositoryMock.Setup(r => r.GetUserOrganizationIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());
        _membershipRepositoryMock.Setup(r => r.GetUserProjectIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
        
        // Verify it exited early without counting
        _taskRepositoryMock.Verify(r => r.CountAsync(It.IsAny<IQueryable<TaskItem>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private TaskItem CreateTaskWithTitle(int id, Guid projectId, Project project, string title)
    {
        var task = TaskItem.Create(projectId, null, null, null, "reporter", title, null, default, default, null, null, DateTime.UtcNow);
        typeof(TaskItem).GetProperty("Id")!.SetValue(task, id);
        typeof(TaskItem).GetProperty("Project")!.SetValue(task, project);
        typeof(TaskItem).GetProperty("CreatedAt")!.SetValue(task, DateTime.UtcNow.AddDays(-id));
        return task;
    }
}