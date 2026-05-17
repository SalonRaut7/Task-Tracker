using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Features.Projects.Commands.CreateProject;
using TaskTracker.Application.Features.Projects.Commands.UpdateProject;
using TaskTracker.Domain.Constants;
using TaskTracker.Tests.TaskTracker.API.IntegrationTests.Infrastructure;

namespace TaskTracker.Tests.TaskTracker.API.IntegrationTests.Features.Projects;

public class ProjectsControllerIntegrationTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public ProjectsControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ThenGetById_ShouldPersistProject_WhenRequestIsValid()
    {
        var seedProjectId = await _factory.EnsureProjectAsync("PRJA");
        var organizationId = await _factory.GetOrganizationIdForProjectAsync(seedProjectId);
        var client = await _factory.CreateAuthenticatedClientAsync();

        var createCommand = new CreateProjectCommand
        {
            OrganizationId = organizationId,
            Name = "Roadmap",
            Key = "ROAD",
            Description = "Project created via integration test"
        };

        var createResponse = await client.PostAsJsonAsync("/api/projects", createCommand);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdProject = await createResponse.Content.ReadFromJsonAsync<ProjectResponseDto>();
        Assert.NotNull(createdProject);
        createdProject!.OrganizationId.Should().Be(organizationId);
        createdProject.Name.Should().Be("Roadmap");
        createdProject.Key.Should().Be("ROAD");

        var getResponse = await client.GetAsync($"/api/projects/{createdProject.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedProject = await getResponse.Content.ReadFromJsonAsync<ProjectResponseDto>();
        Assert.NotNull(fetchedProject);
        fetchedProject!.Id.Should().Be(createdProject.Id);
        fetchedProject.Name.Should().Be("Roadmap");
    }

    [Fact]
    public async Task Update_ShouldModifyProject_WhenRequestIsValid()
    {
        var projectId = await _factory.EnsureProjectAsync("PRJB");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var updateCommand = new UpdateProjectCommand
        {
            Name = "Updated Project Name",
            Key = "UPDKEY",
            Description = "Updated description"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/projects/{projectId}", updateCommand);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedProject = await updateResponse.Content.ReadFromJsonAsync<ProjectResponseDto>();
        Assert.NotNull(updatedProject);
        updatedProject!.Id.Should().Be(projectId);
        updatedProject.Name.Should().Be("Updated Project Name");
        updatedProject.Key.Should().Be("UPDKEY");

        var getResponse = await client.GetAsync($"/api/projects/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetchedProject = await getResponse.Content.ReadFromJsonAsync<ProjectResponseDto>();
        Assert.NotNull(fetchedProject);
        fetchedProject!.Name.Should().Be("Updated Project Name");
        fetchedProject.Key.Should().Be("UPDKEY");
    }

    [Fact]
    public async Task Delete_ShouldRemoveProject_WhenRequestIsValid()
    {
        var projectId = await _factory.EnsureProjectAsync("PRJC");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var deleteResponse = await client.DeleteAsync($"/api/projects/{projectId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/projects/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
    {
        var seedProjectId = await _factory.EnsureProjectAsync("PRJD");
        var organizationId = await _factory.GetOrganizationIdForProjectAsync(seedProjectId);
        var client = await _factory.CreateAuthenticatedClientAsync();

        var invalidCommand = new CreateProjectCommand
        {
            OrganizationId = organizationId,
            Name = string.Empty,
            Key = "lowercase",
            Description = "invalid"
        };

        var response = await client.PostAsJsonAsync("/api/projects", invalidCommand);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var details = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(details);
        details!.Errors.Should().ContainKey("Name");
        details.Errors.Should().ContainKey("Key");
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_ForViewerOrganizationMember()
    {
        var seedProjectId = await _factory.EnsureProjectAsync("PRJE");
        var organizationId = await _factory.GetOrganizationIdForProjectAsync(seedProjectId);
        var viewerClient = await _factory.CreateProjectMemberClientAsync(seedProjectId, AppRoles.Viewer);

        var createCommand = new CreateProjectCommand
        {
            OrganizationId = organizationId,
            Name = "Viewer Attempt",
            Key = "VIEW",
            Description = "Viewer should not be able to create"
        };

        var response = await viewerClient.PostAsJsonAsync("/api/projects", createCommand);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ShouldReturnForbidden_ForViewerProjectMember()
    {
        var projectId = await _factory.EnsureProjectAsync("PRJF");
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);

        var updateCommand = new UpdateProjectCommand
        {
            Name = "Not allowed",
            Key = "NAKEY",
            Description = "Viewer cannot update"
        };

        var response = await viewerClient.PutAsJsonAsync($"/api/projects/{projectId}", updateCommand);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_ForViewerProjectMember()
    {
        var projectId = await _factory.EnsureProjectAsync("PRJG");
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);

        var response = await viewerClient.DeleteAsync($"/api/projects/{projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed class ProjectResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
    }
}
