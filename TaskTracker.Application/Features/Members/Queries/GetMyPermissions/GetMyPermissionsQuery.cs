using MediatR;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Features.Members.Queries.GetMyPermissions;

public sealed class GetMyPermissionsQuery : IRequest<UserPermissionsDto> { }
