using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Tests.TaskTracker.API.IntegrationTests.Infrastructure;

namespace TaskTracker.Tests.TaskTracker.API.IntegrationTests.Features.TaskAttachments;

public class TaskAttachmentsControllerIntegrationTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public TaskAttachmentsControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetByTask_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/taskattachments?taskId=1");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByTask_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/taskattachments?taskId=999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Upload_ThenGetByTask_ThenDelete_ShouldWork_WhenRequestIsValid()
    {
        var projectId = await _factory.EnsureProjectAsync("ATA");
        var (taskId, _) = await _factory.EnsureTaskAsync(projectId, "Attachment test task");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var uploadResponse = await client.PostAsync(
            "/api/taskattachments",
            BuildMultipartUpload(taskId, "notes.txt", "text/plain", "hello attachment"));

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uploadedAttachment = await uploadResponse.Content.ReadFromJsonAsync<TaskAttachmentDto>();
        Assert.NotNull(uploadedAttachment);
        uploadedAttachment!.TaskId.Should().Be(taskId);
        uploadedAttachment.FileName.Should().Be("notes.txt");

        var listResponse = await client.GetAsync($"/api/taskattachments?taskId={taskId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var attachments = await listResponse.Content.ReadFromJsonAsync<List<TaskAttachmentDto>>();
        Assert.NotNull(attachments);
        attachments!.Should().Contain(attachment => attachment.Id == uploadedAttachment.Id);

        var deleteResponse = await client.DeleteAsync($"/api/taskattachments/{uploadedAttachment.Id}?taskId={taskId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listAfterDeleteResponse = await client.GetAsync($"/api/taskattachments?taskId={taskId}");
        listAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var attachmentsAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<List<TaskAttachmentDto>>();
        Assert.NotNull(attachmentsAfterDelete);
        attachmentsAfterDelete!.Should().NotContain(attachment => attachment.Id == uploadedAttachment.Id);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenValidationFails()
    {
        var projectId = await _factory.EnsureProjectAsync("ATB");
        var (taskId, _) = await _factory.EnsureTaskAsync(projectId, "Invalid attachment test task");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync(
            "/api/taskattachments",
            BuildMultipartUpload(taskId, "virus.exe", "application/octet-stream", "bad"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var details = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(details);
        details!.Errors.Should().ContainKey("FileName");
    }

    [Fact]
    public async Task Upload_ShouldReturnForbidden_ForViewerProjectMember()
    {
        var projectId = await _factory.EnsureProjectAsync("ATC");
        var (taskId, _) = await _factory.EnsureTaskAsync(projectId, "Viewer attachment test task");
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);

        var response = await viewerClient.PostAsync(
            "/api/taskattachments",
            BuildMultipartUpload(taskId, "viewer-note.txt", "text/plain", "viewer cannot upload"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_ForViewerProjectMember()
    {
        var projectId = await _factory.EnsureProjectAsync("ATD");
        var (taskId, _) = await _factory.EnsureTaskAsync(projectId, "Viewer delete test task");
        var adminClient = await _factory.CreateAuthenticatedClientAsync();
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);

        var uploadResponse = await adminClient.PostAsync(
            "/api/taskattachments",
            BuildMultipartUpload(taskId, "deletable.txt", "text/plain", "delete me"));

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploadedAttachment = await uploadResponse.Content.ReadFromJsonAsync<TaskAttachmentDto>();
        Assert.NotNull(uploadedAttachment);

        var response = await viewerClient.DeleteAsync($"/api/taskattachments/{uploadedAttachment!.Id}?taskId={taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Download_ShouldReturnNotFound_WhenAttachmentDoesNotExist()
    {
        var projectId = await _factory.EnsureProjectAsync("ATE");
        var (taskId, _) = await _factory.EnsureTaskAsync(projectId, "Download test task");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/taskattachments/{Guid.NewGuid()}/download?taskId={taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static MultipartFormDataContent BuildMultipartUpload(
        int taskId,
        string fileName,
        string contentType,
        string content)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(taskId.ToString()), "taskId");

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);

        return form;
    }
}
