using MainAPI.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MainAPI.ServiceCollections;

public static class HealthCheckServiceCollection
{
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Health Checks
        var healthCheckSettings = configuration.GetSection("HealthChecks");
        if (healthCheckSettings.GetValue<bool>("EnableHealthChecks", true))
        {
            var redisConnectionString = configuration["Redis:ConnectionString"] ?? "redis:6379";
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });

            if (!string.IsNullOrEmpty(connectionString))
            {
                services.AddHealthChecks()
                    .AddDbContextCheck<ApplicationDbContext>("database", tags: new[] { "ready" });
            }
        }

        return services;
    }
}