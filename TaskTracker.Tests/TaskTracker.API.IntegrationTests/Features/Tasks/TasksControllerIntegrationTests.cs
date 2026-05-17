using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Features.Tasks.Commands.CreateTask;
using TaskTracker.Application.Features.Tasks.Commands.UpdateTask;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Tests.TaskTracker.API.IntegrationTests.Infrastructure;

namespace TaskTracker.Tests.TaskTracker.API.IntegrationTests.Features.Tasks;

public class TasksControllerIntegrationTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public TasksControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        // Arrange
        var client = await _factory.CreateAuthenticatedClientAsync();
        var nonExistentTaskId = 9999;
        var projectId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/tasks/{nonExistentTaskId}?projectId={projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ThenGetById_ShouldPersistTask_WhenRequestIsValid()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTA");
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Act
        var createdTask = await CreateTaskAsync(client, projectId, "Integration test task");

        // Assert
        createdTask.Title.Should().Be("Integration test task");
        createdTask.ProjectId.Should().Be(projectId);
        createdTask.TaskCode.Should().StartWith("INTA-");

        var getResponse = await client.GetAsync($"/api/tasks/{createdTask.Id}?projectId={projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        Assert.NotNull(fetchedTask);
        fetchedTask!.Id.Should().Be(createdTask.Id);
        fetchedTask.Title.Should().Be(createdTask.Title);
        fetchedTask.ProjectId.Should().Be(projectId);
    }

    [Fact]
    public async Task Update_ShouldModifyTask_WhenRequestIsValid()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTB");
        var client = await _factory.CreateAuthenticatedClientAsync();
        var createdTask = await CreateTaskAsync(client, projectId, "Original title");

        var updateCommand = new UpdateTaskCommand
        {
            Title = "Updated title",
            Description = "Updated description",
            Status = Status.InProgress,
            Priority = TaskPriority.High,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3))
        };

        // Act
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/tasks/{createdTask.Id}?projectId={projectId}",
            updateCommand);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedTask = await updateResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        Assert.NotNull(updatedTask);
        updatedTask!.Id.Should().Be(createdTask.Id);
        updatedTask.Title.Should().Be("Updated title");

        var getResponse = await client.GetAsync($"/api/tasks/{createdTask.Id}?projectId={projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        Assert.NotNull(fetchedTask);
        fetchedTask!.Title.Should().Be("Updated title");
    }

    [Fact]
    public async Task Delete_ShouldRemoveTask_WhenRequestIsValid()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTC");
        var client = await _factory.CreateAuthenticatedClientAsync();
        var createdTask = await CreateTaskAsync(client, projectId, "Task to delete");

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/tasks/{createdTask.Id}?projectId={projectId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/tasks/{createdTask.Id}?projectId={projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTD");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var invalidCommand = new CreateTaskCommand
        {
            ProjectId = projectId,
            Title = string.Empty,
            Description = "Invalid payload",
            Status = Status.NotStarted,
            Priority = TaskPriority.Medium,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/tasks", invalidCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);

        problemDetails!.Errors.Should().ContainKey("Title");
        problemDetails.Errors.Should().ContainKey("StartDate");
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_ForViewerProjectMember()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTE");
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);

        var command = BuildValidCreateCommand(projectId, "Viewer cannot create");

        // Act
        var response = await viewerClient.PostAsJsonAsync("/api/tasks", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_ForViewerProjectMember()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTF");
        var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);
        var createdTask = await CreateTaskAsync(adminClient, projectId, "Viewer cannot update");

        var updateCommand = new UpdateTaskCommand
        {
            Title = "Attempted update",
            Description = "No permission",
            Status = Status.InProgress,
            Priority = TaskPriority.High,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))
        };

        // Act
        var response = await viewerClient.PutAsJsonAsync(
            $"/api/tasks/{createdTask.Id}?projectId={projectId}",
            updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_ForViewerProjectMember()
    {
        // Arrange
        var projectId = await _factory.EnsureProjectAsync("INTG");
        var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);
        var createdTask = await CreateTaskAsync(adminClient, projectId, "Viewer cannot delete");

        // Act
        var response = await viewerClient.DeleteAsync($"/api/tasks/{createdTask.Id}?projectId={projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static CreateTaskCommand BuildValidCreateCommand(Guid projectId, string title)
    {
        return new CreateTaskCommand
        {
            ProjectId = projectId,
            Title = title,
            Description = "Created through API integration test.",
            Status = Status.NotStarted,
            Priority = TaskPriority.Medium,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))
        };
    }

    private static async Task<TaskResponseDto> CreateTaskAsync(HttpClient client, Guid projectId, string title)
    {
        var command = BuildValidCreateCommand(projectId, title);
        var createResponse = await client.PostAsJsonAsync("/api/tasks", command);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        Assert.NotNull(createdTask);
        return createdTask!;
    }

    private sealed class TaskResponseDto
    {
        public int Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string TaskCode { get; set; } = string.Empty;
    }
}
