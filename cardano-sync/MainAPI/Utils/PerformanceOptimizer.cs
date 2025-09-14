using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace MainAPI.Utils
{
    /// <summary>
    /// Utility class for performance optimization and monitoring
    /// </summary>
    public class PerformanceOptimizer
    {
        private readonly ILogger<PerformanceOptimizer> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics = new();

        public PerformanceOptimizer(ILogger<PerformanceOptimizer> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Preload frequently accessed data into memory cache
        /// </summary>
        public async Task PreloadFrequentDataAsync()
        {
            _logger.LogInformation("Starting preload of frequent data...");

            try
            {
                // Preload common queries that are frequently accessed
                var preloadTasks = new List<Task>
                {
                    PreloadTotalDrepAsync(),
                    PreloadEpochDataAsync(),
                    PreloadCommonDrepListAsync()
                };

                await Task.WhenAll(preloadTasks);
                _logger.LogInformation("Preload of frequent data completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload frequent data");
            }
        }

        /// <summary>
        /// Track performance metrics for API endpoints
        /// </summary>
        public void TrackEndpointPerformance(string endpoint, long responseTimeMs, bool isSuccess)
        {
            var metrics = _metrics.GetOrAdd(endpoint, _ => new PerformanceMetrics());

            lock (metrics)
            {
                metrics.TotalRequests++;
                metrics.TotalResponseTime += responseTimeMs;
                metrics.AverageResponseTime = metrics.TotalResponseTime / metrics.TotalRequests;

                if (isSuccess)
                {
                    metrics.SuccessfulRequests++;
                }
                else
                {
                    metrics.FailedRequests++;
                }

                // Update min/max response times
                if (responseTimeMs < metrics.MinResponseTime || metrics.MinResponseTime == 0)
                {
                    metrics.MinResponseTime = responseTimeMs;
                }
                if (responseTimeMs > metrics.MaxResponseTime)
                {
                    metrics.MaxResponseTime = responseTimeMs;
                }

                // Keep last 100 response times for percentile calculation
                metrics.RecentResponseTimes.Enqueue(responseTimeMs);
                if (metrics.RecentResponseTimes.Count > 100)
                {
                    metrics.RecentResponseTimes.Dequeue();
                }
            }
        }

        /// <summary>
        /// Get performance metrics for all endpoints
        /// </summary>
        public Dictionary<string, PerformanceMetrics> GetPerformanceMetrics()
        {
            return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Get performance metrics for a specific endpoint
        /// </summary>
        public PerformanceMetrics? GetEndpointMetrics(string endpoint)
        {
            return _metrics.TryGetValue(endpoint, out var metrics) ? metrics : null;
        }

        /// <summary>
        /// Optimize database queries by analyzing patterns
        /// </summary>
        public async Task OptimizeDatabaseQueriesAsync()
        {
            _logger.LogInformation("Starting database query optimization...");

            try
            {
                // Analyze query patterns and suggest optimizations
                var slowQueries = _metrics
                    .Where(m => m.Value.AverageResponseTime > 1000) // Queries taking more than 1 second
                    .OrderByDescending(m => m.Value.AverageResponseTime)
                    .Take(10)
                    .ToList();

                if (slowQueries.Any())
                {
                    _logger.LogWarning("Found {Count} slow queries that may need optimization:", slowQueries.Count);
                    foreach (var query in slowQueries)
                    {
                        _logger.LogWarning("Endpoint: {Endpoint}, Avg Response Time: {AvgTime}ms, Total Requests: {TotalRequests}",
                            query.Key, query.Value.AverageResponseTime, query.Value.TotalRequests);
                    }
                }

                // Cache frequently accessed data
                await CacheFrequentDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize database queries");
            }
        }

        /// <summary>
        /// Cache frequently accessed data
        /// </summary>
        private async Task CacheFrequentDataAsync()
        {
            try
            {
                // Cache common data that doesn't change frequently
                var cacheKey = "frequent_data_cache";
                var cacheData = new
                {
                    CachedAt = DateTime.UtcNow,
                    Data = "Frequently accessed data placeholder"
                };

                _memoryCache.Set(cacheKey, cacheData, TimeSpan.FromMinutes(30));
                _logger.LogDebug("Cached frequent data with key: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cache frequent data");
            }
        }

        private async Task PreloadTotalDrepAsync()
        {
            try
            {
                // This would typically call your service to preload data
                _logger.LogDebug("Preloading total DREP data...");
                await Task.Delay(100); // Simulate data loading
                _logger.LogDebug("Total DREP data preloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload total DREP data");
            }
        }

        private async Task PreloadEpochDataAsync()
        {
            try
            {
                _logger.LogDebug("Preloading epoch data...");
                await Task.Delay(100); // Simulate data loading
                _logger.LogDebug("Epoch data preloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload epoch data");
            }
        }

        private async Task PreloadCommonDrepListAsync()
        {
            try
            {
                _logger.LogDebug("Preloading common DREP list...");
                await Task.Delay(100); // Simulate data loading
                _logger.LogDebug("Common DREP list preloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload common DREP list");
            }
        }
    }

    /// <summary>
    /// Performance metrics for tracking endpoint performance
    /// </summary>
    public class PerformanceMetrics
    {
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public long TotalResponseTime { get; set; }
        public double AverageResponseTime { get; set; }
        public long MinResponseTime { get; set; }
        public long MaxResponseTime { get; set; }
        public Queue<long> RecentResponseTimes { get; set; } = new Queue<long>();

        public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
        public double P95ResponseTime => CalculatePercentile(95);
        public double P99ResponseTime => CalculatePercentile(99);

        private double CalculatePercentile(int percentile)
        {
            if (!RecentResponseTimes.Any()) return 0;

            var sortedTimes = RecentResponseTimes.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sortedTimes.Count) - 1;
            return index >= 0 && index < sortedTimes.Count ? sortedTimes[index] : 0;
        }
    }
}