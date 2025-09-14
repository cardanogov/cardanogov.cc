using Serilog;

namespace MainAPI.ServiceCollections;

public static class LoggingServiceCollection
{
    public static IServiceCollection AddLoggingServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        return services;
    }

    public static IHostBuilder UseLoggingServices(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog();
    }
}