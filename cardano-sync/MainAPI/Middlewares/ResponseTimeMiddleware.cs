using System.Diagnostics;

namespace MainAPI.Middlewares;

public class ResponseTimeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseTimeMiddleware> _logger;

    public ResponseTimeMiddleware(RequestDelegate next, ILogger<ResponseTimeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var responseTime = stopwatch.ElapsedMilliseconds;
            var responseTimeFormatted = $"{responseTime}ms";

            // Only add headers if response hasn't started yet
            if (!context.Response.HasStarted)
            {
                // Add response time header
                context.Response.Headers["X-Response-Time"] = responseTimeFormatted;

                // Get cache status from context items (set by CacheService)
                var cacheStatus = context.Items.TryGetValue("CacheStatus", out var status) ? status?.ToString() : "UNKNOWN";
                context.Response.Headers["X-Cache-Status"] = cacheStatus ?? "UNKNOWN";
            }

            // Log response time for monitoring (always log regardless of response status)
            _logger.LogInformation(
                "Request {Method} {Path} completed in {ResponseTime}ms with status {StatusCode} (Cache: {CacheStatus})",
                context.Request.Method,
                context.Request.Path,
                responseTime,
                context.Response.StatusCode,
                context.Items.TryGetValue("CacheStatus", out var logStatus) ? logStatus?.ToString() : "UNKNOWN"
            );
        }
    }
}

// Extension method for easy registration
public static class ResponseTimeMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseTime(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseTimeMiddleware>();
    }
}