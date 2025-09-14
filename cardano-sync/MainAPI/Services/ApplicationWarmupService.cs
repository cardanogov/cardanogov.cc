using MainAPI.Core.Interfaces;
using SharedLibrary.Interfaces;

namespace MainAPI.Services
{
    /// <summary>
    /// Hosted service to warmup application on startup
    /// </summary>
    public class ApplicationWarmupService : IHostedService
    {
        private readonly ILogger<ApplicationWarmupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ApplicationWarmupService(
            ILogger<ApplicationWarmupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting application warmup service...");

            try
            {
                // Create a scope for the warmup operations
                using var scope = _serviceProvider.CreateScope();

                await PerformWarmupAsync(scope.ServiceProvider, cancellationToken);

                _logger.LogInformation("Application warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application warmup failed");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application warmup service stopped");
            return Task.CompletedTask;
        }

        private async Task PerformWarmupAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Starting application warmup...");

            try
            {
                // Get required services
                var cacheService = serviceProvider.GetService<ICacheService>();
                var drepService = serviceProvider.GetService<IDrepService>();

                var warmupTasks = new List<Task>();

                // Warmup Redis connection
                if (cacheService != null)
                {
                    warmupTasks.Add(WarmupRedisAsync(cacheService, cancellationToken));
                }

                // Warmup Database connection
                if (drepService != null)
                {
                    warmupTasks.Add(WarmupDatabaseAsync(drepService, cancellationToken));
                }

                // Wait for all warmup tasks to complete
                await Task.WhenAll(warmupTasks);

                stopwatch.Stop();
                _logger.LogInformation("Application warmup completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Application warmup failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task WarmupRedisAsync(ICacheService cacheService, CancellationToken cancellationToken)
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

        private async Task WarmupDatabaseAsync(IDrepService drepService, CancellationToken cancellationToken)
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
}