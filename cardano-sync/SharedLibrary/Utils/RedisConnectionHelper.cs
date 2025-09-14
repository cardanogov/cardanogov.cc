using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SharedLibrary.Utils;

public static class RedisConnectionHelper
{
    public static IConnectionMultiplexer CreateRedisConnection(string connectionString, ILogger? logger = null)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { connectionString },
            AbortOnConnectFail = false,  // Don't abort on connect fail
            ConnectRetry = 5,
            ReconnectRetryPolicy = new ExponentialRetry(5000),
            ConnectTimeout = 15000,      // Increased to 15 seconds
            SyncTimeout = 15000,         // Increased to 15 seconds
            ResponseTimeout = 15000,     // Increased to 15 seconds
            KeepAlive = 180,             // Keep connection alive
            DefaultDatabase = 0
        };

        var redis = ConnectionMultiplexer.Connect(options);

        // Test connection with retry
        var maxRetries = 10;
        var retryDelay = 2000; // 2 seconds

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                redis.GetDatabase().Ping();
                logger?.LogInformation("Successfully connected to Redis at {ConnectionString}", connectionString);
                return redis;
            }
            catch (Exception ex)
            {
                logger?.LogWarning("Redis connection attempt {Attempt}/{MaxAttempts} failed: {Message}",
                    i + 1, maxRetries, ex.Message);

                if (i < maxRetries - 1)
                {
                    Thread.Sleep(retryDelay);
                    retryDelay = Math.Min(retryDelay * 2, 10000); // Exponential backoff, max 10s
                }
            }
        }

        logger?.LogWarning("Failed to connect to Redis after {MaxRetries} attempts. Service will continue with retry logic.", maxRetries);
        return redis; // Return the connection anyway, it will retry automatically
    }
}