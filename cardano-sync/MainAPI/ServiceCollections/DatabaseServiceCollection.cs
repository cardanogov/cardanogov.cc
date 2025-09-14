using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MainAPI.ServiceCollections;

public static class DatabaseServiceCollection
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Database Context
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            // Register MainAPI ApplicationDbContext (for backward compatibility)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Register EF Core's built-in DbContextFactory for independent instances
            // Use the overload that doesn't depend on scoped DbContextOptions
            services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, options) =>
                options.UseNpgsql(connectionString), ServiceLifetime.Scoped);
        }

        return services;
    }
}