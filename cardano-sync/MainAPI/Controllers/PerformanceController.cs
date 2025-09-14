using MainAPI.Models;
using MainAPI.Utils;
using Microsoft.AspNetCore.Mvc;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceController : BaseController
    {
        private readonly PerformanceOptimizer _performanceOptimizer;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            PerformanceOptimizer performanceOptimizer,
            ILogger<PerformanceController> logger) : base(logger)
        {
            _performanceOptimizer = performanceOptimizer;
            _logger = logger;
        }

        /// <summary>
        /// Get performance metrics for all endpoints
        /// </summary>
        [HttpGet("metrics")]
        public ActionResult<ApiResponse<object>> GetPerformanceMetrics()
        {
            try
            {
                var metrics = _performanceOptimizer.GetPerformanceMetrics();

                var result = new
                {
                    TotalEndpoints = metrics.Count,
                    Endpoints = metrics.Select(kvp => new
                    {
                        Endpoint = kvp.Key,
                        TotalRequests = kvp.Value.TotalRequests,
                        SuccessfulRequests = kvp.Value.SuccessfulRequests,
                        FailedRequests = kvp.Value.FailedRequests,
                        SuccessRate = $"{kvp.Value.SuccessRate:F2}%",
                        AverageResponseTime = $"{kvp.Value.AverageResponseTime:F2}ms",
                        MinResponseTime = $"{kvp.Value.MinResponseTime}ms",
                        MaxResponseTime = $"{kvp.Value.MaxResponseTime}ms",
                        P95ResponseTime = $"{kvp.Value.P95ResponseTime:F2}ms",
                        P99ResponseTime = $"{kvp.Value.P99ResponseTime:F2}ms"
                    }).OrderByDescending(x => x.TotalRequests).ToList()
                };

                return Success((object)result, "Performance metrics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Error retrieving performance metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Get performance metrics for a specific endpoint
        /// </summary>
        [HttpGet("metrics/{endpoint}")]
        public ActionResult<ApiResponse<object>> GetEndpointMetrics(string endpoint)
        {
            try
            {
                var metrics = _performanceOptimizer.GetEndpointMetrics(endpoint);

                if (metrics == null)
                {
                    return NotFound<object>($"No metrics found for endpoint: {endpoint}");
                }

                var result = new
                {
                    Endpoint = endpoint,
                    TotalRequests = metrics.TotalRequests,
                    SuccessfulRequests = metrics.SuccessfulRequests,
                    FailedRequests = metrics.FailedRequests,
                    SuccessRate = $"{metrics.SuccessRate:F2}%",
                    AverageResponseTime = $"{metrics.AverageResponseTime:F2}ms",
                    MinResponseTime = $"{metrics.MinResponseTime}ms",
                    MaxResponseTime = $"{metrics.MaxResponseTime}ms",
                    P95ResponseTime = $"{metrics.P95ResponseTime:F2}ms",
                    P99ResponseTime = $"{metrics.P99ResponseTime:F2}ms",
                    RecentResponseTimes = metrics.RecentResponseTimes.Take(10).ToList()
                };

                return Success((object)result, "Endpoint metrics retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Error retrieving endpoint metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Get slow endpoints that may need optimization
        /// </summary>
        [HttpGet("slow-endpoints")]
        public ActionResult<ApiResponse<object>> GetSlowEndpoints([FromQuery] int thresholdMs = 1000)
        {
            try
            {
                var metrics = _performanceOptimizer.GetPerformanceMetrics();
                var slowEndpoints = metrics
                    .Where(m => m.Value.AverageResponseTime > thresholdMs)
                    .OrderByDescending(m => m.Value.AverageResponseTime)
                    .Select(kvp => new
                    {
                        Endpoint = kvp.Key,
                        AverageResponseTime = $"{kvp.Value.AverageResponseTime:F2}ms",
                        TotalRequests = kvp.Value.TotalRequests,
                        SuccessRate = $"{kvp.Value.SuccessRate:F2}%",
                        P95ResponseTime = $"{kvp.Value.P95ResponseTime:F2}ms"
                    })
                    .ToList();

                var result = new
                {
                    ThresholdMs = thresholdMs,
                    SlowEndpointsCount = slowEndpoints.Count,
                    SlowEndpoints = slowEndpoints
                };

                return Success((object)result, "Slow endpoints analysis completed");
            }
            catch (Exception ex)
            {
                return Error<object>($"Error analyzing slow endpoints: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger performance optimization
        /// </summary>
        [HttpPost("optimize")]
        public async Task<ActionResult<ApiResponse<object>>> OptimizePerformance()
        {
            try
            {
                _logger.LogInformation("Manual performance optimization triggered");

                // Preload frequent data
                await _performanceOptimizer.PreloadFrequentDataAsync();

                // Optimize database queries
                await _performanceOptimizer.OptimizeDatabaseQueriesAsync();

                var result = new
                {
                    Message = "Performance optimization completed",
                    Timestamp = DateTime.UtcNow,
                    Optimizations = new[]
                    {
                        "Frequent data preloaded",
                        "Database queries analyzed",
                        "Cache optimized"
                    }
                };

                return Success((object)result, "Performance optimization completed successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Error during performance optimization: {ex.Message}");
            }
        }

        /// <summary>
        /// Get performance summary
        /// </summary>
        [HttpGet("summary")]
        public ActionResult<ApiResponse<object>> GetPerformanceSummary()
        {
            try
            {
                var metrics = _performanceOptimizer.GetPerformanceMetrics();

                if (!metrics.Any())
                {
                    return Success((object)new { Message = "No performance data available yet" }, "No metrics available");
                }

                var totalRequests = metrics.Sum(m => m.Value.TotalRequests);
                var totalSuccessful = metrics.Sum(m => m.Value.SuccessfulRequests);
                var totalFailed = metrics.Sum(m => m.Value.FailedRequests);
                var overallSuccessRate = totalRequests > 0 ? (double)totalSuccessful / totalRequests * 100 : 0;
                var avgResponseTime = metrics.Average(m => m.Value.AverageResponseTime);
                var maxResponseTime = metrics.Max(m => m.Value.MaxResponseTime);
                var slowEndpoints = metrics.Count(m => m.Value.AverageResponseTime > 1000);

                var result = new
                {
                    TotalEndpoints = metrics.Count,
                    TotalRequests = totalRequests,
                    SuccessfulRequests = totalSuccessful,
                    FailedRequests = totalFailed,
                    OverallSuccessRate = $"{overallSuccessRate:F2}%",
                    AverageResponseTime = $"{avgResponseTime:F2}ms",
                    MaxResponseTime = $"{maxResponseTime}ms",
                    SlowEndpoints = slowEndpoints,
                    TopEndpoints = metrics
                        .OrderByDescending(m => m.Value.TotalRequests)
                        .Take(5)
                        .Select(m => new
                        {
                            Endpoint = m.Key,
                            Requests = m.Value.TotalRequests,
                            AvgTime = $"{m.Value.AverageResponseTime:F2}ms"
                        })
                        .ToList()
                };

                return Success((object)result, "Performance summary retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<object>($"Error retrieving performance summary: {ex.Message}");
            }
        }
    }
}