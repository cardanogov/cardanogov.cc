namespace MainAPI.Services
{
    /// <summary>
    /// Scheduled service to warmup all APIs daily at 00:01 UTC
    /// </summary>
    public class ScheduledWarmupService : IHostedService, IDisposable
    {
        private readonly ILogger<ScheduledWarmupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer? _timer;
        private readonly HttpClient _httpClient;

        public ScheduledWarmupService(
            ILogger<ScheduledWarmupService> logger,
            IServiceProvider serviceProvider,
            HttpClient httpClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpClient = httpClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting scheduled warmup service...");

            // Get configuration
            using var scope = _serviceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetService<IConfiguration>();

            var enableScheduledWarmup = configuration?.GetValue<bool>("ScheduledWarmup:EnableScheduledWarmup", true);
            if (!enableScheduledWarmup.GetValueOrDefault(true))
            {
                _logger.LogInformation("Scheduled warmup is disabled in configuration");
                return Task.CompletedTask;
            }

            // Get warmup time from configuration (default: 00:01:00)
            var warmupTimeStr = configuration?.GetValue<string>("ScheduledWarmup:WarmupTime", "00:01:00");
            if (!TimeSpan.TryParse(warmupTimeStr, out var warmupTime))
            {
                warmupTime = TimeSpan.FromMinutes(1); // Default to 00:01:00
            }

            // Calculate time until next warmup time UTC
            var now = DateTime.UtcNow;
            var nextRun = now.Date.Add(warmupTime);

            // If it's already past the warmup time today, schedule for tomorrow
            if (now.TimeOfDay >= warmupTime)
            {
                nextRun = now.Date.AddDays(1).Add(warmupTime);
            }

            var timeUntilNextRun = nextRun - now;

            _logger.LogInformation("Next scheduled warmup will run at {NextRun} UTC (in {TimeUntilNextRun})",
                nextRun.ToString("yyyy-MM-dd HH:mm:ss"), timeUntilNextRun);

            _timer = new Timer(DoWarmup, null, timeUntilNextRun, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping scheduled warmup service...");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async void DoWarmup(object? state)
        {
            _logger.LogInformation("Starting scheduled API warmup at {Time} UTC", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                using var scope = _serviceProvider.CreateScope();
                await PerformScheduledWarmupAsync(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled warmup failed");
            }
        }

        private async Task PerformScheduledWarmupAsync(IServiceProvider serviceProvider)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var warmupResults = new List<string>();

            try
            {
                // Get configuration
                var configuration = serviceProvider.GetService<IConfiguration>();
                var baseUrl = configuration?.GetValue<string>("ScheduledWarmup:BaseUrl", "https://localhost:5001");
                var requestTimeout = configuration?.GetValue<int>("ScheduledWarmup:RequestTimeoutSeconds", 30);
                var maxConcurrentRequests = configuration?.GetValue<int>("ScheduledWarmup:MaxConcurrentRequests", 10);
                var enableLogging = configuration?.GetValue<bool>("ScheduledWarmup:EnableLogging", true);

                // List of all API endpoints to warmup
                var apiEndpoints = GetApiEndpoints();

                if (enableLogging.GetValueOrDefault(true))
                {
                    _logger.LogInformation("Warming up {EndpointCount} API endpoints with max {MaxConcurrent} concurrent requests",
                        apiEndpoints.Count, maxConcurrentRequests);
                }

                // Use SemaphoreSlim to limit concurrent requests
                using var semaphore = new SemaphoreSlim(maxConcurrentRequests.GetValueOrDefault(10));
                var warmupTasks = new List<Task<string>>();

                foreach (var endpoint in apiEndpoints)
                {
                    warmupTasks.Add(WarmupEndpointWithSemaphoreAsync(endpoint, baseUrl, requestTimeout.GetValueOrDefault(30), semaphore));
                }

                // Wait for all warmup tasks to complete
                var results = await Task.WhenAll(warmupTasks);

                foreach (var result in results)
                {
                    warmupResults.Add(result);
                }

                stopwatch.Stop();

                if (enableLogging.GetValueOrDefault(true))
                {
                    _logger.LogInformation("Scheduled warmup completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                    // Log summary
                    var successCount = warmupResults.Count(r => r.Contains("SUCCESS"));
                    var failureCount = warmupResults.Count(r => r.Contains("FAILED"));

                    _logger.LogInformation("Warmup Summary: {SuccessCount} successful, {FailureCount} failed out of {TotalCount} endpoints",
                        successCount, failureCount, apiEndpoints.Count);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Scheduled warmup failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        private async Task<string> WarmupEndpointWithSemaphoreAsync(string endpoint, string baseUrl, int timeoutSeconds, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            try
            {
                return await WarmupEndpointAsync(endpoint, baseUrl, timeoutSeconds);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<string> WarmupEndpointAsync(string endpoint, string baseUrl, int timeoutSeconds = 30)
        {
            var fullUrl = $"{baseUrl}{endpoint}";
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

                // Add timeout to prevent hanging requests
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                var response = await _httpClient.SendAsync(request, cts.Token);

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    return $"{endpoint}: SUCCESS ({stopwatch.ElapsedMilliseconds}ms)";
                }
                else
                {
                    return $"{endpoint}: FAILED - HTTP {response.StatusCode} ({stopwatch.ElapsedMilliseconds}ms)";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return $"{endpoint}: FAILED - {ex.Message} ({stopwatch.ElapsedMilliseconds}ms)";
            }
        }

        private List<string> GetApiEndpoints()
        {
            return new List<string>
            {
                // Account endpoints
                "/api/account",
                "/api/account/balance",
                "/api/account/transactions",
                
                // API Key endpoints
                "/api/apikey",
                "/api/apikey/validate",
                
                // Committee endpoints
                "/api/committee",
                "/api/committee/members",
                "/api/committee/activities",
                
                // Combine endpoints
                "/api/combine",
                "/api/combine/dashboard",
                "/api/combine/statistics",
                
                // Drep endpoints
                "/api/drep",
                "/api/drep/list",
                "/api/drep/statistics",
                "/api/drep/activities",
                "/api/drep/voting-history",
                
                // Epoch endpoints
                "/api/epoch",
                "/api/epoch/current",
                "/api/epoch/history",
                
                // Image endpoints
                "/api/image",
                "/api/image/pool",
                "/api/image/drep",
                
                // Pool endpoints
                "/api/pool",
                "/api/pool/list",
                "/api/pool/statistics",
                "/api/pool/performance",
                
                // Price endpoints
                "/api/price",
                "/api/price/ada",
                "/api/price/history",
                
                // Proposal endpoints
                "/api/proposal",
                "/api/proposal/list",
                "/api/proposal/details",
                "/api/proposal/voting",
                
                // Treasury endpoints
                "/api/treasury",
                "/api/treasury/balance",
                "/api/treasury/transactions",
                
                // Voting endpoints
                "/api/voting",
                "/api/voting/active",
                "/api/voting/history",
                "/api/voting/statistics",
                
                // Performance endpoints
                "/api/performance",
                "/api/performance/metrics",
                "/api/performance/cache-status",
                
                // Queue health endpoints
                "/api/queuehealth",
                "/api/queuehealth/status",
                "/api/queuehealth/metrics",
                
                // Warmup and health endpoints
                "/api/warmup",
                "/api/warmup/health",
                "/health",
                "/health/ready",
                "/health/live",
                
                // Test endpoints
                "/test-cache"
            };
        }
    }
}