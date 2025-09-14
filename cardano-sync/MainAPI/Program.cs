
using MainAPI.Core.Interfaces;
using MainAPI.Middlewares;
using MainAPI.ServiceCollections;
using MainAPI.Services;
using MainAPI.Utils;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// Add HttpContextAccessor for cache status tracking
builder.Services.AddHttpContextAccessor();

// Add all application services
builder.Services.AddApplicationServices(builder.Configuration);

// Add performance optimization services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PerformanceOptimizer>();

// Add hosted services
builder.Services.AddHostedService<ApplicationWarmupService>();
builder.Services.AddHostedService<ScheduledWarmupService>();

// Add HttpClient for scheduled warmup
builder.Services.AddHttpClient<ScheduledWarmupService>();

var app = builder.Build();

// Performance monitoring middleware
app.UsePerformanceMonitoring();

// Response time middleware should be early to capture timing for all requests
app.UseResponseTime();

// Custom middleware registration
app.UseMiddleware<ErrorLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyRateLimitMiddleware>();
app.UseMiddleware<AuthHeaderMiddleware>();

// Add CORS - Remove this line as it conflicts with the specific origins policy below

// Add cache testing endpoint (remove this in production)
app.MapGet("/test-cache", async (ICacheService cacheService) =>
{
    var testData = new
    {
        Id = 1,
        Name = "Test Object",
        Description = "This is a test object to verify cache optimization",
        Items = new[] { "item1", "item2", "item3" },
        Timestamp = DateTime.UtcNow
    };

    // Test caching
    await cacheService.SetAsync("test-key", testData, 3600);
    var cachedData = await cacheService.GetAsync<object>("test-key");

    return Results.Ok(new
    {
        Original = testData,
        Cached = cachedData,
        Message = "Cache test completed successfully"
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        // Build a swagger endpoint for each discovered API version
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }

        options.RoutePrefix = string.Empty;
        options.DocumentTitle = "Cardano Main API Documentation";
        options.DisplayRequestDuration();
    });
}

// Use CORS
app.UseCors("AllowSpecificOrigins");

// Only API Key Rate Limiting is used - no IP rate limiting needed

app.UseHttpsRedirection();

// Use Health Checks
var healthCheckSettings = builder.Configuration.GetSection("HealthChecks");
if (healthCheckSettings.GetValue<bool>("EnableHealthChecks", true))
{
    app.UseHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description
                })
            });
            await context.Response.WriteAsync(result);
        }
    });

    app.UseHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.UseHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });
}

app.UseAuthorization();

app.MapControllers();

// Log startup information
Log.Information("Starting Cardano Main API v1.0 with Clean Architecture and API Key Rate Limiting");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
Log.Information("Application started successfully");

app.Run();
