using MainAPI.Core.Interfaces;
using MainAPI.Models;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Interfaces;
using System.Diagnostics;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarmupController : BaseController
    {
        private readonly IDrepService _drepService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<WarmupController> _logger;

        public WarmupController(
            IDrepService drepService,
            ICacheService cacheService,
            ILogger<WarmupController> logger) : base(logger)
        {
            _drepService = drepService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Warmup endpoint to initialize services and connections
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> Warmup()
        {
            var stopwatch = Stopwatch.StartNew();
            var warmupResults = new List<string>();

            try
            {
                _logger.LogInformation("Starting application warmup...");

                // 1. Test Redis connection
                try
                {
                    await _cacheService.SetAsync("warmup_test", "test_value", 60);
                    var testValue = await _cacheService.GetAsync<string>("warmup_test");
                    await _cacheService.RemoveAsync("warmup_test");
                    warmupResults.Add("Redis connection: OK");
                    _logger.LogInformation("Redis connection test completed");
                }
                catch (Exception ex)
                {
                    warmupResults.Add($"Redis connection: FAILED - {ex.Message}");
                    _logger.LogWarning(ex, "Redis connection test failed");
                }

                // 3. Test database connection through service
                try
                {
                    // Make a simple service call to test database connection
                    var totalDrep = await _drepService.GetTotalDrepAsync();
                    warmupResults.Add("Database connection: OK");
                    _logger.LogInformation("Database connection test completed");
                }
                catch (Exception ex)
                {
                    warmupResults.Add($"Database connection: FAILED - {ex.Message}");
                    _logger.LogWarning(ex, "Database connection test failed");
                }

                stopwatch.Stop();

                var result = new
                {
                    WarmupTime = $"{stopwatch.ElapsedMilliseconds}ms",
                    Results = warmupResults,
                    Status = warmupResults.All(r => r.Contains("OK")) ? "Ready" : "Partial",
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Application warmup completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                return Success((object)result, "Application warmup completed");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Warmup failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return Error<object>($"Warmup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger manual warmup of all API endpoints
        /// </summary>
        [HttpPost("trigger")]
        public async Task<ActionResult<ApiResponse<object>>> TriggerWarmup()
        {
            var stopwatch = Stopwatch.StartNew();
            var warmupResults = new List<string>();

            try
            {
                _logger.LogInformation("Manual warmup triggered at {Time} UTC", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

                // Get base URL from configuration
                var configuration = HttpContext.RequestServices.GetService<IConfiguration>();
                var baseUrl = configuration?.GetValue<string>("ScheduledWarmup:BaseUrl", "https://localhost:5001");
                var requestTimeout = configuration?.GetValue<int>("ScheduledWarmup:RequestTimeoutSeconds", 30);
                var maxConcurrentRequests = configuration?.GetValue<int>("ScheduledWarmup:MaxConcurrentRequests", 10);

                // List of all API endpoints to warmup
                var apiEndpoints = GetApiEndpointsForWarmup();

                _logger.LogInformation("Manual warmup: Warming up {EndpointCount} API endpoints", apiEndpoints.Count);

                // Use SemaphoreSlim to limit concurrent requests
                using var semaphore = new SemaphoreSlim(maxConcurrentRequests.GetValueOrDefault(10));
                var warmupTasks = new List<Task<string>>();

                foreach (var endpoint in apiEndpoints)
                {
                    warmupTasks.Add(WarmupEndpointWithSemaphoreAsync(endpoint, baseUrl, requestTimeout.GetValueOrDefault(30), semaphore));
                }

                // Wait for all warmup tasks to complete
                var results = await Task.WhenAll(warmupTasks);

                foreach (var r in results)
                {
                    warmupResults.Add(r);
                }

                stopwatch.Stop();

                var successCount = warmupResults.Count(r => r.Contains("SUCCESS"));
                var failureCount = warmupResults.Count(r => r.Contains("FAILED"));

                var result = new
                {
                    WarmupTime = $"{stopwatch.ElapsedMilliseconds}ms",
                    TotalEndpoints = apiEndpoints.Count,
                    SuccessfulEndpoints = successCount,
                    FailedEndpoints = failureCount,
                    Results = warmupResults,
                    Status = failureCount == 0 ? "All Successful" : "Partial Success",
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Manual warmup completed in {ElapsedMs}ms: {SuccessCount} successful, {FailureCount} failed",
                    stopwatch.ElapsedMilliseconds, successCount, failureCount);

                return Success((object)result, "Manual warmup completed");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Manual warmup failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return Error<object>($"Manual warmup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Health check endpoint with detailed service status
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<ApiResponse<object>>> HealthCheck()
        {
            var healthResults = new Dictionary<string, object>();

            try
            {
                // Check Redis
                try
                {
                    var redisStart = Stopwatch.StartNew();
                    await _cacheService.SetAsync("health_check", "test", 10);
                    var redisValue = await _cacheService.GetAsync<string>("health_check");
                    await _cacheService.RemoveAsync("health_check");
                    redisStart.Stop();

                    healthResults["Redis"] = new
                    {
                        Status = "Healthy",
                        ResponseTime = $"{redisStart.ElapsedMilliseconds}ms",
                        Value = redisValue
                    };
                }
                catch (Exception ex)
                {
                    healthResults["Redis"] = new
                    {
                        Status = "Unhealthy",
                        Error = ex.Message
                    };
                }

                // Check Database
                try
                {
                    var dbStart = Stopwatch.StartNew();
                    var totalDrep = await _drepService.GetTotalDrepAsync();
                    dbStart.Stop();

                    healthResults["Database"] = new
                    {
                        Status = "Healthy",
                        ResponseTime = $"{dbStart.ElapsedMilliseconds}ms",
                        TotalDrep = totalDrep
                    };
                }
                catch (Exception ex)
                {
                    healthResults["Database"] = new
                    {
                        Status = "Unhealthy",
                        Error = ex.Message
                    };
                }

                var overallStatus = healthResults.All(h => h.Value.ToString().Contains("Healthy")) ? "Healthy" : "Degraded";

                var result = new
                {
                    Status = overallStatus,
                    Services = healthResults,
                    Timestamp = DateTime.UtcNow
                };

                return Success((object)result, "Health check completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Error<object>($"Health check failed: {ex.Message}");
            }
        }

        private List<string> GetApiEndpointsForWarmup()
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
                using var httpClient = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

                // Add timeout to prevent hanging requests
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

                var response = await httpClient.SendAsync(request, cts.Token);

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
    }
}