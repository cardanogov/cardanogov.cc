using Polly;
using Polly.Extensions.Http;

namespace MainAPI.ServiceCollections;

// Handler để tự động thêm KOIOS Authorization header
public class KoiosAuthHandler : DelegatingHandler
{
    private readonly string _koiosToken;

    public KoiosAuthHandler(IConfiguration configuration)
    {
        _koiosToken = configuration["KOIOS"] ?? string.Empty;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Tự động thêm Authorization header nếu chưa có và có token
        if (!string.IsNullOrEmpty(_koiosToken) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Add("Authorization", $"Bearer {_koiosToken}");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

public static class HttpClientServiceCollection
{
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services)
    {
        // Register KoiosAuthHandler
        services.AddTransient<KoiosAuthHandler>();

        // Add HTTP Client with Polly
        services.AddHttpClient("default")
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Add Koios API HTTP Client with automatic auth header
        services.AddHttpClient("koios", client =>
        {
            client.BaseAddress = new Uri("https://api.koios.rest/");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<KoiosAuthHandler>()
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    // Polly policies
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}