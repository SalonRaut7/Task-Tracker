using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;
using TaskTracker.Application.Features.Tasks.Queries.GetTaskById;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;
using Xunit;
using FluentAssertions;

namespace TaskTracker.Tests.TaskTracker.Application.UnitTests.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQueryHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IMembershipRepository> _membershipRepositoryMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly GetTaskByIdQueryHandler _handler;

    private readonly Guid _projectId = Guid.NewGuid();
    private readonly int _taskId = 1;

    public GetTaskByIdQueryHandlerTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _membershipRepositoryMock = new Mock<IMembershipRepository>();

        // Setup UserManager Mock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _handler = new GetTaskByIdQueryHandler(
            _taskRepositoryMock.Object,
            _membershipRepositoryMock.Object,
            _userManagerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTaskDoesNotExist()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = _taskId, ProjectId = _projectId };
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenTaskDoesNotBelongToProject()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = _taskId, ProjectId = _projectId };
        var otherProjectId = Guid.NewGuid();
        
        // Mock a task that belongs to a different project
        var task = TaskItem.Create(otherProjectId, null, null, null, "reporter-1", "Test Task", null, default, default, null, null, DateTime.UtcNow);
        typeof(TaskItem).GetProperty("Id")!.SetValue(task, _taskId);
        
        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldReturnTaskDto_WhenTaskExistsAndBelongsToProject()
    {
        // Arrange
        var query = new GetTaskByIdQuery { Id = _taskId, ProjectId = _projectId };
        var reporterId = "user-1";
        
        var task = TaskItem.Create(
            _projectId, null, null, null, reporterId, "Test Task", "Desc", default, default, null, null, DateTime.UtcNow);
        typeof(TaskItem).GetProperty("Id")!.SetValue(task, _taskId);

        _taskRepositoryMock.Setup(r => r.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _membershipRepositoryMock.Setup(r => r.GetProjectMembershipsAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserProject> 
            { 
                 new UserProject { UserId = reporterId, Role = "Owner", ProjectId = _projectId }
            });

        // Setup UserManager with MockQueryable
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = reporterId, FirstName = "John", LastName = "Doe", IsActive = true }
        };
        var usersMock = users.AsQueryable().BuildMock();
        
        _userManagerMock.Setup(u => u.Users).Returns(usersMock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_taskId, result.Id);
        Assert.Equal("Test Task", result.Title);
        Assert.Equal(_projectId, result.ProjectId);
        
        Assert.NotNull(result.ReporterUser);
        Assert.Equal(reporterId, result.ReporterUser.UserId);
        Assert.Equal("John Doe", result.ReporterUser.FullName);
        Assert.Equal("Owner", result.ReporterUser.Role);
    }
}
