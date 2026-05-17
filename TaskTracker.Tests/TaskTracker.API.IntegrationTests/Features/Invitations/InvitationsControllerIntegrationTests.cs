using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.Invitations.Commands.CreateInvitation;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Tests.TaskTracker.API.IntegrationTests.Infrastructure;

namespace TaskTracker.Tests.TaskTracker.API.IntegrationTests.Features.Invitations;

public class InvitationsControllerIntegrationTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public InvitationsControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetByScope_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var projectId = await _factory.EnsureProjectAsync("IVA");
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/invitations?scopeType=Project&scopeId={projectId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ThenGetByScope_ShouldPersistInvitation_WhenRequestIsValid()
    {
        var projectId = await _factory.EnsureProjectAsync("IVB");
        var invitee = await _factory.CreateRegisteredUserAsync();
        var client = await _factory.CreateAuthenticatedClientAsync();

        var command = new CreateInvitationCommand
        {
            ScopeType = ScopeType.Project,
            ScopeId = projectId,
            InviteeEmail = invitee.Email,
            Role = AppRoles.Viewer
        };

        var createResponse = await client.PostAsJsonAsync("/api/invitations", command);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdInvitation = await createResponse.Content.ReadFromJsonAsync<InvitationDto>();
        Assert.NotNull(createdInvitation);
        createdInvitation!.ScopeId.Should().Be(projectId);
        createdInvitation.ScopeType.Should().Be(ScopeType.Project);
        createdInvitation.InviteeEmail.Should().Be(invitee.Email);
        createdInvitation.Role.Should().Be(AppRoles.Viewer);
        createdInvitation.Status.Should().Be(InvitationStatus.Pending);

        var listResponse = await client.GetAsync($"/api/invitations?scopeType=Project&scopeId={projectId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitations = await listResponse.Content.ReadFromJsonAsync<List<InvitationDto>>();
        Assert.NotNull(invitations);
        invitations!.Should().Contain(i => i.Id == createdInvitation.Id);
    }

    [Fact]
    public async Task Revoke_ShouldSetInvitationStatusToRevoked_WhenInvitationExists()
    {
        var projectId = await _factory.EnsureProjectAsync("IVC");
        var invitee = await _factory.CreateRegisteredUserAsync();
        var client = await _factory.CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/invitations", new CreateInvitationCommand
        {
            ScopeType = ScopeType.Project,
            ScopeId = projectId,
            InviteeEmail = invitee.Email,
            Role = AppRoles.Viewer
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdInvitation = await createResponse.Content.ReadFromJsonAsync<InvitationDto>();
        Assert.NotNull(createdInvitation);

        var revokeResponse = await client.PostAsync($"/api/invitations/{createdInvitation!.Id}/revoke", content: null);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync($"/api/invitations?scopeType=Project&scopeId={projectId}");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitations = await listResponse.Content.ReadFromJsonAsync<List<InvitationDto>>();
        Assert.NotNull(invitations);

        var revokedInvitation = invitations!.Single(i => i.Id == createdInvitation.Id);
        revokedInvitation.Status.Should().Be(InvitationStatus.Revoked);
    }

    [Fact]
    public async Task Resend_ShouldReturnOk_WhenInvitationExists()
    {
        var projectId = await _factory.EnsureProjectAsync("IVD");
        var invitee = await _factory.CreateRegisteredUserAsync();
        var client = await _factory.CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/invitations", new CreateInvitationCommand
        {
            ScopeType = ScopeType.Project,
            ScopeId = projectId,
            InviteeEmail = invitee.Email,
            Role = AppRoles.Viewer
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdInvitation = await createResponse.Content.ReadFromJsonAsync<InvitationDto>();
        Assert.NotNull(createdInvitation);

        var resendResponse = await client.PostAsync($"/api/invitations/{createdInvitation!.Id}/resend", content: null);

        resendResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resentInvitation = await resendResponse.Content.ReadFromJsonAsync<InvitationDto>();
        Assert.NotNull(resentInvitation);
        resentInvitation!.Id.Should().Be(createdInvitation.Id);
        resentInvitation.Status.Should().Be(InvitationStatus.Pending);
    }

    [Fact]
    public async Task Accept_ShouldReturnBadRequest_WhenTokenIsInvalid()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/invitations/accept", new { Token = "invalid-token" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(details);
        details!.Title.Should().Be("Invitation could not be accepted.");
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenValidationFails()
    {
        var projectId = await _factory.EnsureProjectAsync("IVE");
        var client = await _factory.CreateAuthenticatedClientAsync();

        var invalidCommand = new CreateInvitationCommand
        {
            ScopeType = ScopeType.Project,
            ScopeId = projectId,
            InviteeEmail = "not-an-email",
            Role = AppRoles.OrgAdmin
        };

        var response = await client.PostAsJsonAsync("/api/invitations", invalidCommand);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var details = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(details);
        details!.Errors.Should().ContainKey("InviteeEmail");
    }

    [Fact]
    public async Task Create_ShouldReturnForbidden_ForViewerProjectMember()
    {
        var projectId = await _factory.EnsureProjectAsync("IVF");
        var invitee = await _factory.CreateRegisteredUserAsync();
        var viewerClient = await _factory.CreateProjectMemberClientAsync(projectId, AppRoles.Viewer);

        var command = new CreateInvitationCommand
        {
            ScopeType = ScopeType.Project,
            ScopeId = projectId,
            InviteeEmail = invitee.Email,
            Role = AppRoles.Viewer
        };

        var response = await viewerClient.PostAsJsonAsync("/api/invitations", command);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
