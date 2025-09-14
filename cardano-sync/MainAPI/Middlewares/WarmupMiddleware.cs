using MainAPI.Core.Interfaces;
using SharedLibrary.Interfaces;
using System.Diagnostics;

namespace MainAPI.Middlewares
{
    /// <summary>
    /// Middleware to automatically warmup application services on startup
    /// </summary>
    public class WarmupMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WarmupMiddleware> _logger;
        private static bool _isWarmedUp = false;
        private static readonly object _warmupLock = new object();

        public WarmupMiddleware(RequestDelegate next, ILogger<WarmupMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Only warmup once per application lifetime
            if (!_isWarmedUp)
            {
                lock (_warmupLock)
                {
                    if (!_isWarmedUp)
                    {
                        // Use the application's service provider instead of the request-scoped one
                        var appServiceProvider = context.RequestServices.GetRequiredService<IServiceProvider>();
                        _ = Task.Run(async () => await PerformWarmupAsync(appServiceProvider));
                        _isWarmedUp = true;
                    }
                }
            }

            await _next(context);
        }

        private async Task PerformWarmupAsync(IServiceProvider serviceProvider)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting automatic application warmup...");

            try
            {
                // Get required services
                var cacheService = serviceProvider.GetService<ICacheService>();

                var drepService = serviceProvider.GetService<IDrepService>();

                var warmupTasks = new List<Task>();

                // Warmup Redis connection
                if (cacheService != null)
                {
                    warmupTasks.Add(WarmupRedisAsync(cacheService));
                }

                // Warmup Database connection
                if (drepService != null)
                {
                    warmupTasks.Add(WarmupDatabaseAsync(drepService));
                }

                // Wait for all warmup tasks to complete
                await Task.WhenAll(warmupTasks);

                stopwatch.Stop();
                _logger.LogInformation("Automatic warmup completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Automatic warmup failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task WarmupRedisAsync(ICacheService cacheService)
        {
            try
            {
                _logger.LogDebug("Warming up Redis connection...");
                await cacheService.SetAsync("warmup_redis", "test", 60);
                var testValue = await cacheService.GetAsync<string>("warmup_redis");
                await cacheService.RemoveAsync("warmup_redis");
                _logger.LogDebug("Redis warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis warmup failed");
            }
        }

        private async Task WarmupDatabaseAsync(IDrepService drepService)
        {
            try
            {
                _logger.LogDebug("Warming up Database connection...");
                var totalDrep = await drepService.GetTotalDrepAsync();
                _logger.LogDebug("Database warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database warmup failed");
            }
        }
    }

    /// <summary>
    /// Extension method to register the warmup middleware
    /// </summary>
    public static class WarmupMiddlewareExtensions
    {
        public static IApplicationBuilder UseWarmup(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WarmupMiddleware>();
        }
    }
}