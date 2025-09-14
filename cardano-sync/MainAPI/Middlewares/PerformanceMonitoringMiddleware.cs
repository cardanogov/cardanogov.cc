using MainAPI.Utils;
using System.Diagnostics;

namespace MainAPI.Middlewares
{
    /// <summary>
    /// Middleware to monitor and track performance metrics for API endpoints
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
        private readonly PerformanceOptimizer _performanceOptimizer;

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger,
            PerformanceOptimizer performanceOptimizer)
        {
            _next = next;
            _logger = logger;
            _performanceOptimizer = performanceOptimizer;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                using var memoryStream = new MemoryStream();
                context.Response.Body = memoryStream;

                // Process the request
                await _next(context);

                // Copy the response back to the original stream
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);

                // Track performance metrics
                stopwatch.Stop();
                var endpoint = $"{context.Request.Method} {context.Request.Path}";
                var isSuccess = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400;

                _performanceOptimizer.TrackEndpointPerformance(endpoint, stopwatch.ElapsedMilliseconds, isSuccess);

                // Log slow requests
                if (stopwatch.ElapsedMilliseconds > 2000) // Log requests taking more than 2 seconds
                {
                    _logger.LogWarning("Slow request detected: {Endpoint} took {ElapsedMs}ms, Status: {StatusCode}",
                        endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
                }
                else if (stopwatch.ElapsedMilliseconds > 1000) // Log requests taking more than 1 second
                {
                    _logger.LogInformation("Moderate request time: {Endpoint} took {ElapsedMs}ms, Status: {StatusCode}",
                        endpoint, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var endpoint = $"{context.Request.Method} {context.Request.Path}";

                _logger.LogError(ex, "Request failed: {Endpoint} failed after {ElapsedMs}ms",
                    endpoint, stopwatch.ElapsedMilliseconds);

                // Track failed request
                _performanceOptimizer.TrackEndpointPerformance(endpoint, stopwatch.ElapsedMilliseconds, false);

                // Restore original body stream
                context.Response.Body = originalBodyStream;

                // Re-throw the exception
                throw;
            }
            finally
            {
                // Ensure original body stream is restored
                context.Response.Body = originalBodyStream;
            }
        }
    }

    /// <summary>
    /// Extension method to register the performance monitoring middleware
    /// </summary>
    public static class PerformanceMonitoringMiddlewareExtensions
    {
        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PerformanceMonitoringMiddleware>();
        }
    }
}