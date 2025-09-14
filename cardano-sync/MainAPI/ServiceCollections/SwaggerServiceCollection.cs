using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MainAPI.ServiceCollections;

public static class SwaggerServiceCollection
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        // Add Routing with lowercase URLs
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true; // Chuyển URL thành chữ thường
            options.LowercaseQueryStrings = true; // Chuyển query string thành chữ thường
        });

        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(options =>
        {
            // Add a custom operation filter which sets default values
            options.OperationFilter<SwaggerDefaultValues>();
            // Add custom operation filter for response time
            options.OperationFilter<SwaggerResponseTimeFilter>();
        });

        return services;
    }
}

// Swagger configuration classes
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IConfiguration _configuration;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration)
    {
        _provider = provider;
        _configuration = configuration;
    }

    public void Configure(SwaggerGenOptions options)
    {
        // Add a swagger document for each discovered API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Cardano Main API",
            Version = description.ApiVersion.ToString(),
            Description = "Main API for Cardano blockchain data synchronization with Clean Architecture",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Cardano Development Team",
                Email = "dev@cardano.com"
            },
            License = new Microsoft.OpenApi.Models.OpenApiLicense
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " This API version has been deprecated.";
        }

        return info;
    }
}

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        // REF: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1752#issue-663991077
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }
    }
}

public class SwaggerResponseTimeFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add response time information to operation description
        if (operation.Description == null)
        {
            operation.Description = "";
        }

        operation.Description += "\n\n**Response Time Information:**\n";
        operation.Description += "- **Cache Hit**: ~10-50ms\n";
        operation.Description += "- **Cache Miss (Database)**: ~100-500ms\n";
        operation.Description += "- **Cache Miss (External API)**: ~1-3 seconds\n";
        operation.Description += "- **Complex Queries**: ~2-5 seconds\n\n";
        operation.Description += "*Note: Response times may vary based on data size and server load.*";

        // Add response time headers to all responses
        foreach (var response in operation.Responses.Values)
        {
            if (response.Headers == null)
            {
                response.Headers = new Dictionary<string, OpenApiHeader>();
            }

            // Add X-Response-Time header
            response.Headers["X-Response-Time"] = new OpenApiHeader
            {
                Description = "Response time in milliseconds",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Example = new Microsoft.OpenApi.Any.OpenApiString("123.45ms")
                }
            };

            // Add X-Cache-Status header
            response.Headers["X-Cache-Status"] = new OpenApiHeader
            {
                Description = "Cache status (HIT/MISS)",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                    {
                        new Microsoft.OpenApi.Any.OpenApiString("HIT"),
                        new Microsoft.OpenApi.Any.OpenApiString("MISS")
                    }
                }
            };
        }
    }
}