using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;

namespace TaskTracker.Infrastructure.Services;

// In-process IMemoryCache-backed implementation of ICacheService.
// Registered as Singleton — IMemoryCache is designed for singleton lifetime.
// Tracks all active keys via a ConcurrentDictionary to support RemoveByPrefix
// and diagnostics (IMemoryCache has no native key enumeration API).
public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<MemoryCacheService> _logger;

    // Tracks every key currently known to exist in the cache
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    // Diagnostics counters — thread-safe via Interlocked
    private long _totalHits;
    private long _totalMisses;

    public MemoryCacheService(
        IMemoryCache cache,
        IOptions<CacheOptions> options,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            Interlocked.Increment(ref _totalHits);
            _logger.LogDebug("[Cache HIT]  key={Key}", key);
            return cached!;
        }

        Interlocked.Increment(ref _totalMisses);
        _logger.LogDebug("[Cache MISS] key={Key} → loading from DB", key);

        var value = await factory();

        var entryOptions = BuildEntryOptions(slidingExpiration, absoluteExpiration);

        // Register a post-eviction callback to keep _keys in sync
        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
        });

        _cache.Set(key, value, entryOptions);
        _keys.TryAdd(key, 0);

        return value;
    }

    /// <inheritdoc />
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var entryOptions = BuildEntryOptions(slidingExpiration, absoluteExpiration);

        entryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _keys.TryRemove(evictedKey.ToString()!, out _);
        });

        _cache.Set(key, value, entryOptions);
        _keys.TryAdd(key, 0);

        _logger.LogDebug("[Cache SET]  key={Key}", key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        _logger.LogDebug("[Cache DEL]  key={Key}", key);
    }

    /// <inheritdoc />
    public void RemoveByPrefix(string prefix)
    {
        var removed = 0;
        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
                removed++;
            }
        }

        if (removed > 0)
        {
            _logger.LogDebug("[Cache DEL]  prefix={Prefix} → {Count} entries removed", prefix, removed);
        }
    }

    /// <inheritdoc />
    public CacheDiagnostics GetDiagnostics() =>
        new(
            TotalHits: Interlocked.Read(ref _totalHits),
            TotalMisses: Interlocked.Read(ref _totalMisses),
            TrackedEntries: _keys.Count);

    // Helpers
    private static MemoryCacheEntryOptions BuildEntryOptions(
        TimeSpan? sliding,
        TimeSpan? absolute)
    {
        var opts = new MemoryCacheEntryOptions();

        if (sliding.HasValue)
            opts.SlidingExpiration = sliding;

        if (absolute.HasValue)
            opts.AbsoluteExpirationRelativeToNow = absolute;

        return opts;
    }
}
