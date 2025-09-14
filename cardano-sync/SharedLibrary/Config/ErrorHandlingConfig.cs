namespace SharedLibrary.Config
{
    /// <summary>
    /// Configuration settings for error handling and retry logic
    /// </summary>
    public class ErrorHandlingConfig
    {
        /// <summary>
        /// Redis-related error handling settings
        /// </summary>
        public class RedisSettings
        {
            /// <summary>
            /// Maximum number of retry attempts for Redis operations
            /// </summary>
            public int MaxRetryAttempts { get; set; } = 3;

            /// <summary>
            /// Base delay in milliseconds for Redis retry operations
            /// </summary>
            public int BaseRetryDelayMs { get; set; } = 1000;

            /// <summary>
            /// Maximum delay in milliseconds for Redis retry operations
            /// </summary>
            public int MaxRetryDelayMs { get; set; } = 10000;

            /// <summary>
            /// Delay multiplier for exponential backoff
            /// </summary>
            public double ExponentialBackoffMultiplier { get; set; } = 2.0;

            /// <summary>
            /// Connection retry delay in milliseconds for Redis connection issues
            /// </summary>
            public int ConnectionRetryDelayMs { get; set; } = 2000;
        }

        /// <summary>
        /// HTTP-related error handling settings
        /// </summary>
        public class HttpSettings
        {
            /// <summary>
            /// Maximum number of retry attempts for HTTP operations
            /// </summary>
            public int MaxRetryAttempts { get; set; } = 3;

            /// <summary>
            /// Base delay in milliseconds for HTTP retry operations
            /// </summary>
            public int BaseRetryDelayMs { get; set; } = 1000;

            /// <summary>
            /// Maximum delay in milliseconds for HTTP retry operations
            /// </summary>
            public int MaxRetryDelayMs { get; set; } = 30000;

            /// <summary>
            /// Delay multiplier for HTTP connection issues
            /// </summary>
            public int ConnectionIssueDelayMultiplier { get; set; } = 2;

            /// <summary>
            /// Delay multiplier for HTTP timeout issues
            /// </summary>
            public int TimeoutDelayMultiplier { get; set; } = 1;

            /// <summary>
            /// List of HTTP status codes that should trigger retry
            /// </summary>
            public List<int> RetryableHttpStatusCodes { get; set; } = new()
            {
                500, // Internal Server Error
                502, // Bad Gateway
                503, // Service Unavailable
                504, // Gateway Timeout
                429  // Too Many Requests
            };

            /// <summary>
            /// List of HTTP error messages that should trigger retry
            /// </summary>
            public List<string> RetryableHttpErrorMessages { get; set; } = new()
            {
                "response ended prematurely",
                "HttpIOException",
                "ResponseEnded",
                "timeout",
                "HttpClient.Timeout"
            };
        }

        /// <summary>
        /// Pool sync job specific settings
        /// </summary>
        public class PoolSyncSettings
        {
            /// <summary>
            /// Maximum number of failed pools to retry with extended timeout
            /// </summary>
            public int MaxFailedPoolsRetry { get; set; } = 10;

            /// <summary>
            /// Extended timeout multiplier for retry attempts
            /// </summary>
            public double ExtendedTimeoutMultiplier { get; set; } = 2.0;

            /// <summary>
            /// Delay between retry batches in milliseconds
            /// </summary>
            public int RetryBatchDelayMs { get; set; } = 30000;
        }

        /// <summary>
        /// Redis error handling settings
        /// </summary>
        public RedisSettings Redis { get; set; } = new();

        /// <summary>
        /// HTTP error handling settings
        /// </summary>
        public HttpSettings Http { get; set; } = new();

        /// <summary>
        /// Pool sync specific settings
        /// </summary>
        public PoolSyncSettings PoolSync { get; set; } = new();

        /// <summary>
        /// Global circuit breaker settings
        /// </summary>
        public class CircuitBreakerSettings
        {
            /// <summary>
            /// Number of consecutive failures before opening circuit
            /// </summary>
            public int FailureThreshold { get; set; } = 5;

            /// <summary>
            /// Duration in milliseconds before attempting to close circuit
            /// </summary>
            public int RecoveryTimeoutMs { get; set; } = 60000;

            /// <summary>
            /// Percentage of successful requests required to close circuit
            /// </summary>
            public double SuccessThreshold { get; set; } = 0.5;
        }

        /// <summary>
        /// Circuit breaker settings
        /// </summary>
        public CircuitBreakerSettings CircuitBreaker { get; set; } = new();
    }
}