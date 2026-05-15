using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
using Xunit;

namespace TaskTracker.Tests.TaskTracker.Application.UnitTests.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<ISprintRepository> _sprintRepositoryMock;
    private readonly Mock<IMembershipRepository> _membershipRepositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IPermissionEvaluator> _permissionEvaluatorMock;
    private readonly CreateTaskCommandHandler _handler;

    private readonly Guid _projectId = Guid.NewGuid();
    private readonly string _userId = "user-123";
    private readonly string _projectKey = "TSK";

    public CreateTaskCommandHandlerTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sprintRepositoryMock = new Mock<ISprintRepository>();
        _membershipRepositoryMock = new Mock<IMembershipRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _permissionEvaluatorMock = new Mock<IPermissionEvaluator>();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(_userId);
        _currentUserServiceMock.Setup(c => c.IsSuperAdmin).Returns(false);

        _handler = new CreateTaskCommandHandler(
            _taskRepositoryMock.Object,
            _sprintRepositoryMock.Object,
            _membershipRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _permissionEvaluatorMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateTaskAndReturnDto_WhenValidCommand()
    {
        // Arrange
        var command = new CreateTaskCommand
        {
            ProjectId = _projectId,
            Title = "Test Task",
            Status = Status.NotStarted,
            Priority = TaskPriority.Medium
        };

        _taskRepositoryMock.Setup(r => r.GetProjectKeyAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectKey);

        _membershipRepositoryMock.Setup(r => r.GetUserProjectIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _projectId });

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, ct) => typeof(TaskItem).GetProperty("Id")!.SetValue(task, 1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Task", result.Title);
        Assert.Equal(_projectId, result.ProjectId);

        _taskRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _taskRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenProjectDoesNotExist()
    {
        // Arrange
        var command = new CreateTaskCommand { ProjectId = _projectId };
        
        _taskRepositoryMock.Setup(r => r.GetProjectKeyAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Project '{_projectId}' was not found.");
    }

    [Fact]
    public async Task Handle_ShouldThrowForbiddenAccessException_WhenUserIsNotProjectMemberAndNotSuperAdmin()
    {
        // Arrange
        var command = new CreateTaskCommand { ProjectId = _projectId };

        _taskRepositoryMock.Setup(r => r.GetProjectKeyAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectKey);

        _membershipRepositoryMock.Setup(r => r.GetUserProjectIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Guid>());

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("You must be a direct member of the project to create tasks there.");
    }

    [Fact]
    public async Task Handle_ShouldBypassProjectMembershipCheck_WhenUserIsSuperAdmin()
    {
        // Arrange
        var command = new CreateTaskCommand { ProjectId = _projectId, Title = "Admin Task" };

        _currentUserServiceMock.Setup(c => c.IsSuperAdmin).Returns(true);
        _taskRepositoryMock.Setup(r => r.GetProjectKeyAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectKey);

        _taskRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()))
            .Callback<TaskItem, CancellationToken>((task, ct) => typeof(TaskItem).GetProperty("Id")!.SetValue(task, 1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _membershipRepositoryMock.Verify(r => r.GetUserProjectIdsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenEpicDoesNotBelongToProject()
    {
        // Arrange
        var epicId = Guid.NewGuid();
        var command = new CreateTaskCommand { ProjectId = _projectId, EpicId = epicId };

        _taskRepositoryMock.Setup(r => r.GetProjectKeyAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectKey);
        _membershipRepositoryMock.Setup(r => r.GetUserProjectIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _projectId });

        _taskRepositoryMock.Setup(r => r.EpicBelongsToProjectAsync(epicId, _projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Epic does not belong to the selected project.");
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenTaskEndDateExceedsSprint()
    {
        // Arrange
        var sprintId = Guid.NewGuid();
        var sprintEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var command = new CreateTaskCommand 
        { 
            ProjectId = _projectId, 
            SprintId = sprintId,
            EndDate = sprintEndDate.AddDays(1) // Task ends AFTER the sprint
        };

        _taskRepositoryMock.Setup(r => r.GetProjectKeyAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_projectKey);
        _membershipRepositoryMock.Setup(r => r.GetUserProjectIdsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { _projectId });
        _taskRepositoryMock.Setup(r => r.SprintBelongsToProjectAsync(sprintId, _projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sprint = Sprint.Create(
            _projectId, "Test Sprint", "Goal", 
            sprintEndDate.AddDays(-14), sprintEndDate, DateTime.UtcNow);
        typeof(Sprint).GetProperty("Id")!.SetValue(sprint, sprintId);
        typeof(Sprint).GetProperty("Status")!.SetValue(sprint, SprintStatus.Active);
        
        _sprintRepositoryMock.Setup(r => r.GetByIdAsync(sprintId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sprint);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Task end date ({command.EndDate}) exceeds the sprint end date ({sprintEndDate}).");
    }
}
