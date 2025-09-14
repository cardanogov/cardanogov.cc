using MainAPI.Core.Interfaces;
using MainAPI.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MainAPI.Infrastructure.Services;

/// <summary>
/// Optimized Redis cache service implementation
/// </summary>
public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _useFireAndForgetCaching;

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _useFireAndForgetCaching = true; // Enable fire-and-forget by default

        // Optimized JSON serialization options for minimal storage
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Keep original property names to reduce overhead
            WriteIndented = false, // Minimize JSON size
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignore null values
            NumberHandling = JsonNumberHandling.AllowReadingFromString, // Allow flexible number parsing
            PropertyNameCaseInsensitive = false, // Reduce processing overhead
            AllowTrailingCommas = false,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public async Task<CacheResult<T>> GetAsync<T>(string key)
    {
        try
        {
            _logger.LogInformation("GetAsync - Cache type: {CacheType}", _cache.GetType().Name);
            var value = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(value))
            {
                _logger.LogInformation("Cache miss for key {Key} (empty or null)", key);
                return new CacheResult<T> { IsHit = false };
            }

            var cacheWrapper = JsonSerializer.Deserialize<CacheWrapper<T>>(value, _jsonOptions);

            if (cacheWrapper == null)
            {
                _logger.LogInformation("Cache miss for key {Key} (failed to deserialize)", key);
                return new CacheResult<T> { IsHit = false };
            }

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (cacheWrapper.Expiry > 0 && currentTime > cacheWrapper.Expiry)
            {
                _logger.LogInformation("Cache expired for key {Key}, removing from cache", key);
                await _cache.RemoveAsync(key);
                return new CacheResult<T> { IsHit = false };
            }

            _logger.LogInformation("Cache hit for key {Key}: {Result}", key, cacheWrapper.Data);
            return new CacheResult<T>
            {
                IsHit = true,
                Value = cacheWrapper.Data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return new CacheResult<T> { IsHit = false };
        }
    }


    public async Task SetAsync<T>(string key, T value, int? expiration = null)
    {
        try
        {
            // Create cache wrapper similar to Express.js format
            var cacheWrapper = new CacheWrapper<T>
            {
                Data = value,
                Expiry = expiration.HasValue
                    ? DateTimeOffset.UtcNow.AddSeconds(expiration.Value).ToUnixTimeMilliseconds()
                    : 0 // 0 means no expiry
            };

            var jsonValue = JsonSerializer.Serialize(cacheWrapper, _jsonOptions);
            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expiration.Value);
            }

            await _cache.SetStringAsync(key, jsonValue, options);
            _logger.LogDebug("Cached value for key: {Key} with size: {Size} bytes", key, jsonValue.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("Removed cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync<T>(string key)
    {
        try
        {
            var value = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(value))
                return false;

            var cacheWrapper = JsonSerializer.Deserialize<CacheWrapper<T>>(value, _jsonOptions);
            if (cacheWrapper == null)
                return false;

            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (cacheWrapper.Expiry > 0 && currentTime > cacheWrapper.Expiry)
            {
                _logger.LogInformation("Cache expired for key {Key}, removing from cache", key);
                await _cache.RemoveAsync(key);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of cache key: {Key}", key);
            return false;
        }
    }


    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int? expiration = null)
    {
        _logger.LogInformation("GetOrSetAsync called for key: {Key}", key);

        // First, try to get from cache
        var result = await GetAsync<T>(key);
        if (result.IsHit)
        {
            _logger.LogInformation("Cache hit for key: {Key}", key);
            SetCacheStatus("HIT");
            return result.Value!;
        }

        _logger.LogInformation("Cache miss for key: {Key}, executing factory", key);
        SetCacheStatus("MISS");

        // Execute factory to get data
        var value = await factory();
        _logger.LogInformation("Factory returned value for key: {Key}", key);

        // Cache based on configuration
        if (_useFireAndForgetCaching)
        {
            // Fire-and-forget caching for better performance
            _ = Task.Run(async () =>
            {
                try
                {
                    await SetAsync(key, value, expiration);
                    _logger.LogInformation("Background cache set successful for key: {Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background cache set failed for key: {Key}", key);
                }
            });
        }
        else
        {
            // Synchronous caching (original behavior)
            await SetAsync(key, value, expiration);
            _logger.LogInformation("Synchronous cache set successful for key: {Key}", key);
        }

        // Return data immediately
        return value;
    }

    public async Task SetMultipleAsync<T>(Dictionary<string, T> keyValuePairs, int? expiration = null)
    {
        var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration));
        await Task.WhenAll(tasks);
        _logger.LogDebug("Set multiple cache keys: {Count}", keyValuePairs.Count);
    }

    public async Task<Dictionary<string, T?>> GetMultipleAsync<T>(IEnumerable<string> keys)
    {
        var result = new Dictionary<string, T?>();
        var tasks = keys.Select(async key =>
        {
            var value = await GetAsync<T>(key);
            return (Key: key, Value: value);
        });

        var results = await Task.WhenAll(tasks);
        foreach (var (key, value) in results)
        {
            result[key] = value.Value!;
        }

        return result;
    }

    public async Task RemoveMultipleAsync(IEnumerable<string> keys)
    {
        var tasks = keys.Select(key => RemoveAsync(key));
        await Task.WhenAll(tasks);
        _logger.LogDebug("Removed multiple cache keys: {Count}", keys.Count());
    }

    public async Task ClearAsync()
    {
        // Note: This is a simplified implementation
        // In a real scenario, you might want to use Redis FLUSHDB command
        // or implement a more sophisticated clearing mechanism
        _logger.LogWarning("Clear cache operation called - this is a simplified implementation");
        await Task.CompletedTask;
    }

    public async Task<object> GetStatisticsAsync()
    {
        // Note: This is a simplified implementation
        // In a real scenario, you would use Redis INFO command
        // to get actual cache statistics
        var stats = new
        {
            Timestamp = DateTime.UtcNow,
            Message = "Cache statistics not implemented in this simplified version",
            CacheType = "Redis"
        };

        return await Task.FromResult(stats);
    }

    /// <summary>
    /// Sets cache status for response headers
    /// </summary>
    /// <param name="status">Cache status (HIT/MISS)</param>
    private void SetCacheStatus(string status)
    {
        // Store cache status in HttpContext.Items for middleware to access
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            httpContext.Items["CacheStatus"] = status;
        }
    }

    /// <summary>
    /// Enable or disable fire-and-forget caching
    /// </summary>
    /// <param name="enabled">True to enable fire-and-forget, false for synchronous caching</param>
    public void SetFireAndForgetCaching(bool enabled)
    {
        _useFireAndForgetCaching = enabled;
        _logger.LogInformation("Fire-and-forget caching {Status}", enabled ? "enabled" : "disabled");
    }

    /// <summary>
    /// Get current fire-and-forget caching status
    /// </summary>
    /// <returns>True if fire-and-forget is enabled</returns>
    public bool IsFireAndForgetCachingEnabled()
    {
        return _useFireAndForgetCaching;
    }
}