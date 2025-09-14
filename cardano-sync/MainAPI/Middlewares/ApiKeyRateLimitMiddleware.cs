using SharedLibrary.Interfaces;
using SharedLibrary.Models;
using System.Text.Json;

namespace MainAPI.Middlewares
{
    public class ApiKeyRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyRateLimitMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public ApiKeyRateLimitMiddleware(RequestDelegate next, ILogger<ApiKeyRateLimitMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
        {
            try
            {
                var clientIp = GetClientIpAddress(context);
                _logger.LogDebug("Processing request from IP: {IpAddress} for path: {Path}",
                    clientIp, context.Request.Path);

                // Skip rate limiting for health checks, swagger, and localhost
                if (ShouldSkipRateLimit(context))
                {
                    _logger.LogDebug("Skipping rate limit check for request from IP: {IpAddress}", clientIp);
                    await _next(context);
                    return;
                }

                // Extract API key from header
                var apiKey = ExtractApiKey(context);
                var endpoint = $"{context.Request.Method}:{context.Request.Path}";

                RateLimitInfo rateLimitInfo;
                bool isAnonymousUser = false;

                if (string.IsNullOrEmpty(apiKey))
                {
                    // No API key provided - use anonymous/free session
                    rateLimitInfo = await apiKeyService.CheckAnonymousRateLimitAsync(clientIp, endpoint);
                    isAnonymousUser = true;

                    _logger.LogInformation("Anonymous request from IP {IpAddress} - remaining: {Remaining}",
                        clientIp, rateLimitInfo.RemainingRequests);
                }
                else
                {
                    // API key provided - validate and check rate limit
                    if (!await apiKeyService.ValidateApiKeyAsync(apiKey))
                    {
                        await ReturnUnauthorizedResponse(context, "Invalid or expired API key");
                        return;
                    }

                    rateLimitInfo = await apiKeyService.CheckRateLimitAsync(apiKey, endpoint);
                }

                // Check rate limit
                if (rateLimitInfo.IsLimitExceeded)
                {
                    await ReturnRateLimitExceededResponse(context, rateLimitInfo, isAnonymousUser);
                    return;
                }

                // Add rate limit headers
                AddRateLimitHeaders(context, rateLimitInfo, isAnonymousUser);

                // Increment request count
                if (isAnonymousUser)
                {
                    await apiKeyService.IncrementAnonymousRequestCountAsync(clientIp);
                }
                else if (!string.IsNullOrEmpty(apiKey))
                {
                    await apiKeyService.IncrementRequestCountAsync(apiKey);
                }

                // Continue with the request
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in API key rate limit middleware");
                await ReturnInternalServerErrorResponse(context);
            }
        }

        private bool ShouldSkipRateLimit(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Skip rate limiting for health checks, swagger, and localhost
            if (path?.Contains("/health") == true ||
                path?.Contains("/swagger") == true ||
                path?.Contains("/api-docs") == true)
            {
                return true;
            }

            // Skip rate limiting for localhost requests
            var clientIp = GetClientIpAddress(context);
            // if (IsLocalhost(clientIp))
            // {
            //     _logger.LogInformation("Skipping rate limit for localhost request from IP: {IpAddress}", clientIp);
            //     return true;
            // }

            // Skip rate limiting for allowed frontend origins (similar to CORS)
            if (IsAllowedFrontendOrigin(context))
            {
                var origin = context.Request.Headers["Origin"].FirstOrDefault();
                _logger.LogInformation("Skipping rate limit for frontend request from origin: {Origin}", origin);
                return true;
            }

            return false;
        }

        private bool IsLocalhost(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;

            // Check for localhost variations
            var localhostPatterns = new[]
            {
                "127.0.0.1",
                "::1",
                "localhost",
                "127.0.0.0/8",
                "::1/128"
            };

            return localhostPatterns.Any(pattern =>
                ipAddress.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                ipAddress.StartsWith("127.", StringComparison.OrdinalIgnoreCase) ||
                ipAddress.StartsWith("::1", StringComparison.OrdinalIgnoreCase));
        }

        private string? ExtractApiKey(HttpContext context)
        {
            // Try to get API key from header
            if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
            {
                return apiKeyHeader.FirstOrDefault();
            }

            // Try to get API key from query string
            if (context.Request.Query.TryGetValue("api_key", out var apiKeyQuery))
            {
                return apiKeyQuery.FirstOrDefault();
            }

            // Note: Removed Authorization header parsing to avoid conflict with AuthHeaderMiddleware
            // Authorization header is reserved for KOIOS API authentication
            return null;
        }

        private void AddRateLimitHeaders(HttpContext context, RateLimitInfo rateLimitInfo, bool isAnonymousUser)
        {
            var dailyLimit = isAnonymousUser
                ? RateLimitConfig.AnonymousSettings.RequestsPerDay
                : RateLimitConfig.Settings[rateLimitInfo.KeyType].RequestsPerDay;

            context.Response.Headers["X-RateLimit-Limit"] = dailyLimit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = rateLimitInfo.RemainingRequests.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = rateLimitInfo.ResetTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            context.Response.Headers["X-RateLimit-Type"] = isAnonymousUser ? "Anonymous" : rateLimitInfo.KeyType.ToString();

            if (isAnonymousUser)
            {
                context.Response.Headers["X-RateLimit-Anonymous"] = "true";
            }
        }

        private async Task ReturnUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Unauthorized",
                message = message,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private async Task ReturnRateLimitExceededResponse(HttpContext context, RateLimitInfo rateLimitInfo, bool isAnonymousUser)
        {
            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";

            var dailyLimit = isAnonymousUser
                ? RateLimitConfig.AnonymousSettings.RequestsPerDay
                : RateLimitConfig.Settings[rateLimitInfo.KeyType].RequestsPerDay;

            var planType = isAnonymousUser ? "Anonymous" : rateLimitInfo.KeyType.ToString();

            var response = new
            {
                error = "Rate Limit Exceeded",
                message = $"Rate limit exceeded for {planType} plan. Limit: {dailyLimit:N0} requests per day",
                reset_time = rateLimitInfo.ResetTime,
                remaining_requests = rateLimitInfo.RemainingRequests,
                key_type = planType,
                timestamp = DateTime.UtcNow,
                suggestion = isAnonymousUser ? "Consider getting an API key for higher limits" : null
            };

            AddRateLimitHeaders(context, rateLimitInfo, isAnonymousUser);
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private async Task ReturnInternalServerErrorResponse(HttpContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Internal Server Error",
                message = "An error occurred while processing your request",
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

        private string GetClientIpAddress(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.FirstOrDefault() ?? context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private bool IsAllowedFrontendOrigin(HttpContext context)
        {
            var origin = context.Request.Headers["Origin"].FirstOrDefault();
            if (string.IsNullOrEmpty(origin))
                return false;

            // Get allowed origins from configuration (same as CORS settings)
            var corsSettings = _configuration.GetSection("CorsSettings");
            var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new string[0];

            // Also check the alternative CORS configuration
            var alternativeCorsSettings = _configuration.GetSection("Cors");
            var alternativeAllowedOrigins = alternativeCorsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new string[0];

            // Combine both arrays
            var allAllowedOrigins = allowedOrigins.Concat(alternativeAllowedOrigins).Distinct().ToArray();

            return allAllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        }
    }
}