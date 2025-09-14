// DEPRECATED: This middleware has been replaced by KoiosAuthHandler in HttpClientServiceCollection
// The KoiosAuthHandler is more specific and only applies to HttpClient "koios" requests
// avoiding conflicts with ApiKeyRateLimitMiddleware that uses Authorization header for API keys

public class AuthHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _koiosToken;

    public AuthHeaderMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _koiosToken = configuration["KOIOS"] ?? string.Empty;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (
            !string.IsNullOrEmpty(_koiosToken)
            && !context.Request.Headers.ContainsKey("Authorization")
        )
        {
            context.Request.Headers.Append("Authorization", $"Bearer {_koiosToken}");
        }
        await _next(context);
    }
}
