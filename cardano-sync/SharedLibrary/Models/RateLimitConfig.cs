namespace SharedLibrary.Models
{
    public class RateLimitConfig
    {
        public static readonly Dictionary<ApiKeyType, RateLimitSettings> Settings = new()
        {
            [ApiKeyType.Free] = new RateLimitSettings
            {
                RequestsPerMinute = 60,
                RequestsPerHour = 1000,
                RequestsPerDay = 10000,
                BurstLimit = 10
            },
            [ApiKeyType.Premium] = new RateLimitSettings
            {
                RequestsPerMinute = 300,
                RequestsPerHour = 10000,
                RequestsPerDay = 100000,
                BurstLimit = 50
            },
            [ApiKeyType.Enterprise] = new RateLimitSettings
            {
                RequestsPerMinute = 1000,
                RequestsPerHour = 50000,
                RequestsPerDay = 1000000,
                BurstLimit = 200
            }
        };

        // Rate limit settings for anonymous users (more restrictive than Free)
        public static readonly RateLimitSettings AnonymousSettings = new RateLimitSettings
        {
            RequestsPerMinute = 10,
            RequestsPerHour = 100,
            RequestsPerDay = 1000,
            BurstLimit = 5
        };
    }

    public class RateLimitSettings
    {
        public int RequestsPerMinute { get; set; }
        public int RequestsPerHour { get; set; }
        public int RequestsPerDay { get; set; }
        public int BurstLimit { get; set; }
    }

    public class RateLimitInfo
    {
        public int RemainingRequests { get; set; }
        public int TotalRequests { get; set; }
        public DateTime ResetTime { get; set; }
        public ApiKeyType KeyType { get; set; }
        public bool IsLimitExceeded { get; set; }
    }

    // Track anonymous user requests by IP
    public class AnonymousRateLimit
    {
        public string IpAddress { get; set; } = string.Empty;
        public int DailyRequests { get; set; } = 0;
        public DateTime LastDailyReset { get; set; } = DateTime.UtcNow;
        public DateTime LastRequestTime { get; set; } = DateTime.UtcNow;
    }
}