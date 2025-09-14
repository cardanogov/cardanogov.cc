using MainAPI.Application.Queries.StakeAddresses;

namespace MainAPI.ServiceCollections;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add MediatR for CQRS
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetTotalStakeAddressesQuery).Assembly));

        // Add all service collections in the correct order
        services.AddLoggingServices(configuration);
        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            });
        services.AddApiVersioningServices();
        services.AddSwaggerServices();
        services.AddInfrastructureServices(configuration);
        services.AddCorsServices(configuration);
        services.AddCacheServices(configuration);
        services.AddDatabaseServices(configuration);
        services.AddHealthCheckServices(configuration);
        services.AddHttpClientServices();
        services.AddEndpointsApiExplorer();

        return services;
    }
}