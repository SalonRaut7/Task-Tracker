using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;

namespace TaskTracker.API.Controllers;

// Exposes cache diagnostics for debugging and observability.
//
// Access rules:
//   1. Only available in Development environment (enforced at middleware level via routing guard)
//   2. Requires SuperAdmin role (enforced by [Authorize] attribute)
// This means cache internals are never exposed in production — not even to SuperAdmins.

[ApiController]
[Route("api/diagnostics/cache")]
[Authorize(Roles = AppRoles.SuperAdmin)]
public class CacheDiagnosticsController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IWebHostEnvironment _env;

    public CacheDiagnosticsController(ICacheService cache, IWebHostEnvironment env)
    {
        _cache = cache;
        _env = env;
    }

    // Returns cache hit/miss statistics and tracked entry count.
    // Only available in Development environment.
    [HttpGet]
    [ProducesResponseType(typeof(CacheDiagnosticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get()
    {
        // Belt-and-suspenders: also gate at code level so even if routing is misconfigured
        // in production, the endpoint returns 404 rather than sensitive data.
        if (!_env.IsDevelopment())
            return NotFound();

        var diag = _cache.GetDiagnostics();

        return Ok(new CacheDiagnosticsResponse(
            TotalHits: diag.TotalHits,
            TotalMisses: diag.TotalMisses,
            HitRatePercent: diag.HitRatePercent,
            TrackedEntries: diag.TrackedEntries
        ));
    }

    // Flushes all cache entries with a given prefix. Useful for manual cache busting during debugging.
    // Only available in Development environment.
    [HttpDelete("prefix")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult EvictByPrefix([FromQuery] string prefix)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(prefix))
            return BadRequest("prefix is required.");

        _cache.RemoveByPrefix(prefix);
        return NoContent();
    }

    // Evicts a specific cache key. Useful for targeted cache busting during debugging.
    // Only available in Development environment.
    [HttpDelete("key")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult EvictByKey([FromQuery] string key)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("key is required.");

        _cache.Remove(key);
        return NoContent();
    }
}

public sealed record CacheDiagnosticsResponse(
    long TotalHits,
    long TotalMisses,
    double HitRatePercent,
    int TrackedEntries);
