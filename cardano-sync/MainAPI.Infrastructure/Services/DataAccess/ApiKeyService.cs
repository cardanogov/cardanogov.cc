using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;
using System.Security.Cryptography;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<ApiKeyService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public ApiKeyService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ApiKeyService> logger, IMemoryCache cache)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _cache = cache;
        }

        public async Task<ApiKey?> GetApiKeyAsync(string key)
        {
            try
            {
                var cacheKey = $"apikey_{key}";
                if (_cache.TryGetValue(cacheKey, out ApiKey? cachedKey))
                {
                    return cachedKey;
                }

                using var context = await _contextFactory.CreateDbContextAsync();
                var apiKey = await context.api_key
                    .AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Key == key && k.IsActive);

                if (apiKey != null)
                {
                    _cache.Set(cacheKey, apiKey, _cacheExpiration);
                }

                return apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key: {Key}", key);
                return null;
            }
        }

        public async Task<bool> ValidateApiKeyAsync(string key)
        {
            try
            {
                var apiKey = await GetApiKeyAsync(key);
                if (apiKey == null) return false;

                // Check if key is expired
                if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key: {Key}", key);
                return false;
            }
        }

        public async Task<RateLimitInfo> CheckRateLimitAsync(string key, string endpoint)
        {
            try
            {
                var apiKey = await GetApiKeyAsync(key);
                if (apiKey == null)
                {
                    return new RateLimitInfo
                    {
                        IsLimitExceeded = true,
                        RemainingRequests = 0,
                        TotalRequests = 0,
                        ResetTime = DateTime.UtcNow.AddMinutes(1),
                        KeyType = ApiKeyType.Free
                    };
                }

                var settings = RateLimitConfig.Settings[apiKey.Type];
                var now = DateTime.UtcNow;

                // Reset daily count if needed
                if (apiKey.LastDailyReset != null && apiKey.LastDailyReset.Value.Date < now.Date)
                {
                    await ResetDailyCountAsync(key);
                    apiKey.DailyRequests = 0;
                    apiKey.LastDailyReset = now;
                }

                var remainingRequests = settings.RequestsPerDay - apiKey.DailyRequests;
                var isExceeded = remainingRequests <= 0;

                return new RateLimitInfo
                {
                    RemainingRequests = Math.Max(0, remainingRequests),
                    TotalRequests = apiKey.DailyRequests,
                    ResetTime = now.Date.AddDays(1),
                    KeyType = apiKey.Type,
                    IsLimitExceeded = isExceeded
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for key: {Key}", key);
                return new RateLimitInfo
                {
                    IsLimitExceeded = true,
                    RemainingRequests = 0,
                    TotalRequests = 0,
                    ResetTime = DateTime.UtcNow.AddMinutes(1),
                    KeyType = ApiKeyType.Free
                };
            }
        }

        public async Task IncrementRequestCountAsync(string key)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var apiKey = await context.api_key.FirstOrDefaultAsync(s => s.Key == key);

                if (apiKey == null) return;

                apiKey.TotalRequests++;
                apiKey.DailyRequests++;
                apiKey.LastUsedAt = DateTime.UtcNow;

                context.api_key.Update(apiKey);
                await context.SaveChangesAsync();

                // Clear cache
                var cacheKey = $"apikey_{key}";
                _cache.Remove(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing request count for key: {Key}", key);
            }
        }

        public async Task<bool> IsRateLimitExceededAsync(string key, string endpoint)
        {
            var rateLimitInfo = await CheckRateLimitAsync(key, endpoint);
            return rateLimitInfo.IsLimitExceeded;
        }

        public async Task<ApiKey?> CreateApiKeyAsync(string name, string description, ApiKeyType type, string? createdBy = null)
        {
            try
            {
                var key = GenerateApiKey();

                var parameters = new ApiKey
                {
                    Key = key,
                    Name = name,
                    Description = description,
                    Type = type,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                using var context = await _contextFactory.CreateDbContextAsync();
                await context.api_key.AddAsync(parameters);
                await context.SaveChangesAsync();

                var apiKey = new ApiKey
                {
                    Id = parameters.Id,
                    Key = key,
                    Name = name,
                    Description = description,
                    Type = type,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = createdBy
                };

                return apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key for name: {Name}", name);
                return null;
            }
        }

        public async Task<bool> DeactivateApiKeyAsync(string key)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var apiKey = await context.api_key.FirstOrDefaultAsync(k => k.Key == key && k.IsActive);

                if (apiKey == null)
                {
                    return false;
                }

                apiKey.IsActive = false;
                context.api_key.Update(apiKey);
                var affected = await context.SaveChangesAsync();

                // Clear cache
                var cacheKey = $"apikey_{key}";
                _cache.Remove(cacheKey);

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating API key: {Key}", key);
                return false;
            }
        }

        public async Task<List<ApiKey>> GetAllApiKeysAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var apiKeys = await context.api_key
                    .AsNoTracking()
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();
                return apiKeys;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all API keys");
                return new List<ApiKey>();
            }
        }

        public async Task<bool> UpdateApiKeyAsync(ApiKey apiKey)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var existApiKey = await context.api_key.FirstOrDefaultAsync(k => k.Id == apiKey.Id);
                if (existApiKey == null)
                    return false;

                existApiKey.Name = apiKey.Name;
                existApiKey.Description = apiKey.Description;
                existApiKey.Type = apiKey.Type;
                existApiKey.IsActive = apiKey.IsActive;
                existApiKey.ExpiresAt = apiKey.ExpiresAt;
                existApiKey.AllowedOrigins = apiKey.AllowedOrigins;
                existApiKey.AllowedEndpoints = apiKey.AllowedEndpoints;

                context.api_key.Update(existApiKey);

                var affected = await context.SaveChangesAsync();

                // Clear cache
                var cacheKey = $"apikey_{apiKey.Key}";
                _cache.Remove(cacheKey);

                return affected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key: {Id}", apiKey.Id);
                return false;
            }
        }

        private async Task ResetDailyCountAsync(string key)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var apiKey = await context.api_key.FirstOrDefaultAsync(k => k.Key == key && k.IsActive);

                if (apiKey == null)
                {
                    return;
                }

                apiKey.DailyRequests = 0;
                apiKey.LastDailyReset = DateTime.UtcNow;

                context.api_key.Update(apiKey);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting daily count for key: {Key}", key);
            }
        }

        private string GenerateApiKey()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        // Anonymous rate limiting methods
        public async Task<RateLimitInfo> CheckAnonymousRateLimitAsync(string ipAddress, string endpoint)
        {
            try
            {
                var cacheKey = $"anonymous_rate_limit_{ipAddress}";
                var settings = RateLimitConfig.AnonymousSettings;
                var now = DateTime.UtcNow;

                // Get or create anonymous rate limit info
                var anonymousRateLimit = _cache.Get<AnonymousRateLimit>(cacheKey);
                if (anonymousRateLimit == null)
                {
                    anonymousRateLimit = new AnonymousRateLimit
                    {
                        IpAddress = ipAddress,
                        DailyRequests = 0,
                        LastDailyReset = now,
                        LastRequestTime = now
                    };
                }

                // Reset daily count if needed
                if (anonymousRateLimit.LastDailyReset.Date < now.Date)
                {
                    anonymousRateLimit.DailyRequests = 0;
                    anonymousRateLimit.LastDailyReset = now;
                }

                var remainingRequests = settings.RequestsPerDay - anonymousRateLimit.DailyRequests;
                var isExceeded = remainingRequests <= 0;

                // Update cache
                _cache.Set(cacheKey, anonymousRateLimit, TimeSpan.FromDays(1));

                return new RateLimitInfo
                {
                    RemainingRequests = Math.Max(0, remainingRequests),
                    TotalRequests = anonymousRateLimit.DailyRequests,
                    ResetTime = now.Date.AddDays(1),
                    KeyType = ApiKeyType.Free, // Anonymous users are treated as Free tier
                    IsLimitExceeded = isExceeded
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking anonymous rate limit for IP: {IpAddress}", ipAddress);
                return new RateLimitInfo
                {
                    IsLimitExceeded = false, // Allow request on error
                    RemainingRequests = RateLimitConfig.AnonymousSettings.RequestsPerDay,
                    TotalRequests = 0,
                    ResetTime = DateTime.UtcNow.AddMinutes(1),
                    KeyType = ApiKeyType.Free
                };
            }
        }

        public async Task IncrementAnonymousRequestCountAsync(string ipAddress)
        {
            try
            {
                var cacheKey = $"anonymous_rate_limit_{ipAddress}";
                var now = DateTime.UtcNow;

                // Get or create anonymous rate limit info
                var anonymousRateLimit = _cache.Get<AnonymousRateLimit>(cacheKey);
                if (anonymousRateLimit == null)
                {
                    anonymousRateLimit = new AnonymousRateLimit
                    {
                        IpAddress = ipAddress,
                        DailyRequests = 0,
                        LastDailyReset = now,
                        LastRequestTime = now
                    };
                }

                // Reset daily count if needed
                if (anonymousRateLimit.LastDailyReset.Date < now.Date)
                {
                    anonymousRateLimit.DailyRequests = 0;
                    anonymousRateLimit.LastDailyReset = now;
                }

                // Increment request count
                anonymousRateLimit.DailyRequests++;
                anonymousRateLimit.LastRequestTime = now;

                // Update cache
                _cache.Set(cacheKey, anonymousRateLimit, TimeSpan.FromDays(1));

                _logger.LogDebug("Incremented anonymous request count for IP {IpAddress}: {Count}",
                    ipAddress, anonymousRateLimit.DailyRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing anonymous request count for IP: {IpAddress}", ipAddress);
            }
        }
    }
}