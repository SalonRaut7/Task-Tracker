using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsSuperAdmin =>
        Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

    public IEnumerable<string> Roles =>
        User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();
}
