using Serilog;

public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
            if (context.Response.StatusCode >= 400)
            {
                Log.Warning("API Error: {Path} returned status code {StatusCode}", context.Request.Path, context.Response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "API Exception: {Path}", context.Request.Path);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An unexpected error occurred.");
        }
    }
}