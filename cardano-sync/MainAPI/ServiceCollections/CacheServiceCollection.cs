namespace MainAPI.ServiceCollections;

public static class CacheServiceCollection
{
    public static IServiceCollection AddCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Redis Cache
        var redisConnectionString = configuration["Redis:ConnectionString"] ?? "redis:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = configuration["Redis:InstanceName"] ?? "CardanoMainAPI:";
        });

        return services;
    }
}