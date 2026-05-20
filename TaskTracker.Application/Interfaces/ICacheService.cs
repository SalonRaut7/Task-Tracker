namespace TaskTracker.Application.Interfaces;

// Abstraction over the in-process cache. Implementations must be thread-safe 
//and registered as Singleton (IMemoryCache lifetime is Singleton by design).
public interface ICacheService
{
    // Gets a cached value or creates it using the provided factory, then caches it.
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    // Explicitly sets a value in the cache, replacing any existing entry.
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    // Removes a single cache entry by key.
    void Remove(string key);

    // Removes all cache entries whose keys start with the given prefix.
    void RemoveByPrefix(string prefix);
    
    /// Returns diagnostic data: hit count, miss count, tracked entry count.
    CacheDiagnostics GetDiagnostics();
}

public sealed record CacheDiagnostics(long TotalHits, long TotalMisses, int TrackedEntries)
{
    public double HitRatePercent => (TotalHits + TotalMisses) == 0
        ? 0
        : Math.Round(100.0 * TotalHits / (TotalHits + TotalMisses), 1);
}
