using MainAPI.Core.Interfaces;
using MainAPI.Infrastructure.Data;
using MainAPI.Infrastructure.Services;
using MainAPI.Infrastructure.Services.DataAccess;
using MainAPI.Infrastructure.Services.ExternalApis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SharedLibrary.Interfaces;
using SharedLibrary.RedisService;
using SharedLibrary.Utils;
using StackExchange.Redis;

namespace MainAPI.ServiceCollections
{
    public static class InfrastructureServiceCollection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Infrastructure Services
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICacheService, CacheService>();

            // External API Services
            services.AddScoped<IPriceService, PriceService>();

            services.AddScoped<IImageService>(provider => new ImageService(
              provider.GetRequiredService<IConfiguration>(),
              provider.GetRequiredService<ILogger<ImageService>>(),
              provider.GetRequiredService<IHttpClientFactory>().CreateClient("deepai"),
              provider.GetRequiredService<ICacheService>(),
              provider.GetRequiredService<ApplicationDbContext>()
          ));

            // Register interfaces to implementations
            services.AddScoped<IDrepService>(provider => new DrepService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<DrepService>>()
            ));
            services.AddScoped<IPoolService>(provider => new PoolService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<PoolService>>(),
                provider.GetRequiredService<IHttpClientFactory>().CreateClient("PoolMetadata")
            ));
            services.AddScoped<IProposalService>(provider => new ProposalService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<ProposalService>>(),
                provider.GetRequiredService<IImageService>()
            ));
            services.AddScoped<IVotingService>(provider => new VotingService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<VotingService>>()
            ));
            services.AddScoped<IAccountService>(provider => new AccountService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<AccountService>>()
            ));
            services.AddScoped<IEpochService>(provider => new EpochService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<EpochService>>()
            ));
            services.AddScoped<ICommitteeService>(provider => new CommitteeService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<CommitteeService>>()
            ));
            services.AddScoped<ITreasuryService>(provider => new TreasuryService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<TreasuryService>>(),
                provider.GetRequiredService<IPriceService>()
            ));
            services.AddScoped<ICombineService>(provider => new CombineService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<CombineService>>()
            ));
            services.AddScoped<IPriceService, PriceService>();
            //services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IApiKeyService>(provider => new ApiKeyService(
                provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
                provider.GetRequiredService<ILogger<ApiKeyService>>(),
                provider.GetRequiredService<IMemoryCache>()
            ));


            var redisConnectionString = configuration["Redis:ConnectionString"];
            var redis = RedisConnectionHelper.CreateRedisConnection(redisConnectionString);
            services.AddSingleton<IConnectionMultiplexer>(redis);
            services.AddSingleton<IRedisRateLimiter, RedisRateLimiter>();

            // HTTP Client with optimized settings for high throughput
            services.AddHttpClient(
                "KoiosApi",
                c =>
                {
                    c.BaseAddress = new Uri("https://api.koios.rest/api/v1/");
                    c.Timeout = TimeSpan.FromSeconds(60); // Increased timeout to 60s
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    MaxConnectionsPerServer = 50, // Increased from default 2 to 50
                    UseCookies = false // Disable cookies for better performance
                });

            // HTTP Client for Pool Metadata
            services.AddHttpClient(
                "PoolMetadata",
                c =>
                {
                    c.Timeout = TimeSpan.FromSeconds(10);
                    c.DefaultRequestHeaders.Add("User-Agent", "Cardano API");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    MaxConnectionsPerServer = 20,
                    UseCookies = false
                });

            // HTTP Client for Adastat API
            services.AddHttpClient(
                "AdastatApi",
                c =>
                {
                    c.BaseAddress = new Uri("https://api.adastat.net/");
                    c.Timeout = TimeSpan.FromSeconds(30);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    MaxConnectionsPerServer = 10,
                    UseCookies = false
                });

            // HTTP Client for Adastat API
            services.AddHttpClient(
                "deepai",
                c =>
                {
                    c.BaseAddress = new Uri("https://api.deepai.org/");
                    c.Timeout = TimeSpan.FromSeconds(60);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    MaxConnectionsPerServer = 50,
                    UseCookies = false
                });

            return services;
        }
    }
}