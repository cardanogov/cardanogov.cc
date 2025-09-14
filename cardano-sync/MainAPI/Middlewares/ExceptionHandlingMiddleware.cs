using MainAPI.Models;
using System.Net;
using System.Text.Json;

namespace MainAPI.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = GetStatusCode(ex);

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            };

            var json = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }
    }

    private int GetStatusCode(Exception ex)
    {
        // Customize mapping as needed
        return ex switch
        {
            ApplicationException => (int)HttpStatusCode.BadGateway, // 502
            ArgumentException => (int)HttpStatusCode.BadRequest,    // 400
            KeyNotFoundException => (int)HttpStatusCode.NotFound,  // 404
            _ => (int)HttpStatusCode.InternalServerError           // 500
        };
    }
}