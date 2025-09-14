using MainAPI.Core.Models;

namespace MainAPI.Core.Interfaces;

/// <summary>
/// Cache service interface for Redis operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get value from cache
    /// </summary>
    Task<CacheResult<T>> GetAsync<T>(string key);

    /// <summary>
    /// Set value in cache
    /// </summary>
    Task SetAsync<T>(string key, T value, int? expiration = null);

    /// <summary>
    /// Remove value from cache
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync<T>(string key);

    /// <summary>
    /// Get or set value with cache-aside pattern
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int? expiration = null);

    /// <summary>
    /// Set multiple values in cache
    /// </summary>
    Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, int? expiration = null);

    /// <summary>
    /// Get multiple values from cache
    /// </summary>
    Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys);

    /// <summary>
    /// Remove multiple keys from cache
    /// </summary>
    Task RemoveMultipleAsync(IEnumerable<string> keys);

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<object> GetStatisticsAsync();

    /// <summary>
    /// Enable or disable fire-and-forget caching
    /// </summary>
    /// <param name="enabled">True to enable fire-and-forget, false for synchronous caching</param>
    void SetFireAndForgetCaching(bool enabled);

    /// <summary>
    /// Get current fire-and-forget caching status
    /// </summary>
    /// <returns>True if fire-and-forget is enabled</returns>
    bool IsFireAndForgetCachingEnabled();
}