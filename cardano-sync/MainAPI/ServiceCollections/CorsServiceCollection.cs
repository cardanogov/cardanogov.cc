namespace MainAPI.ServiceCollections;

public static class CorsServiceCollection
{
    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add CORS
        var corsSettings = configuration.GetSection("CorsSettings");
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins", policy =>
            {
                var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? new string[0];
                var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? new string[0];
                var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? new string[0];

                policy.WithOrigins(allowedOrigins)
                      .WithMethods(allowedMethods)
                      .WithHeaders(allowedHeaders)
                      .AllowCredentials();
            });
        });

        return services;
    }
}