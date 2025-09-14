using MainAPI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MainAPI.Controllers;

/// <summary>
/// Base controller for all API controllers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    private readonly ILogger _logger;

    protected BaseController(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a successful API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Success")
    {
        var response = ApiResponse<T>.SuccessResponse(data, message);
        response.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Create an error API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> Error<T>(string message, List<string>? errors = null, int statusCode = 400)
    {
        var response = ApiResponse<T>.ErrorResponse(message, errors);
        response.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogWarning("API Error: {Message}, Errors: {Errors}, RequestId: {RequestId}",
            message, errors, response.RequestId);

        return statusCode switch
        {
            400 => BadRequest(response),
            401 => Unauthorized(response),
            403 => Forbid(),
            404 => NotFound(response),
            500 => StatusCode(500, response),
            _ => BadRequest(response)
        };
    }

    /// <summary>
    /// Create a not found API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found")
    {
        var response = ApiResponse<T>.NotFoundResponse(message);
        response.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogWarning("Resource not found: {Message}, RequestId: {RequestId}",
            message, response.RequestId);

        return NotFound(response);
    }

    /// <summary>
    /// Create a validation error API response
    /// </summary>
    protected ActionResult<ApiResponse<T>> ValidationError<T>(List<string> errors)
    {
        var response = ApiResponse<T>.ValidationErrorResponse(errors);
        response.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        _logger.LogWarning("Validation error: {Errors}, RequestId: {RequestId}",
            string.Join(", ", errors), response.RequestId);

        return BadRequest(response);
    }

    /// <summary>
    /// Create a paginated API response
    /// </summary>
    protected ActionResult<PaginatedApiResponse<T>> PaginatedSuccess<T>(
        List<T> data,
        int page,
        int pageSize,
        int totalRecords,
        string message = "Success")
    {
        var response = PaginatedApiResponse<T>.SuccessResponse(data, page, pageSize, totalRecords, message);
        response.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Get the current API version
    /// </summary>
    protected string GetApiVersion()
    {
        return HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
    }

    /// <summary>
    /// Log API request
    /// </summary>
    protected void LogRequest(string action, object? parameters = null)
    {
        _logger.LogInformation("API Request: {Action}, Parameters: {@Parameters}, RequestId: {RequestId}",
            action, parameters, Activity.Current?.Id ?? HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Log API response
    /// </summary>
    protected void LogResponse(string action, object? result = null)
    {
        _logger.LogInformation("API Response: {Action}, Result: {@Result}, RequestId: {RequestId}",
            action, result, Activity.Current?.Id ?? HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Get pagination parameters from query string
    /// </summary>
    protected (int page, int pageSize) GetPaginationParameters(int defaultPageSize = 20, int maxPageSize = 100)
    {
        var page = 1;
        var pageSize = defaultPageSize;

        if (int.TryParse(Request.Query["page"], out var pageParam) && pageParam > 0)
        {
            page = pageParam;
        }

        if (int.TryParse(Request.Query["pageSize"], out var pageSizeParam) && pageSizeParam > 0)
        {
            pageSize = Math.Min(pageSizeParam, maxPageSize);
        }

        return (page, pageSize);
    }

    /// <summary>
    /// Get sorting parameters from query string
    /// </summary>
    protected (string sortBy, string sortOrder) GetSortingParameters(string defaultSortBy = "id", string defaultSortOrder = "asc")
    {
        var sortBy = Request.Query["sortBy"].FirstOrDefault() ?? defaultSortBy;
        var sortOrder = Request.Query["sortOrder"].FirstOrDefault()?.ToLower() ?? defaultSortOrder;

        // Validate sort order
        if (sortOrder != "asc" && sortOrder != "desc")
        {
            sortOrder = "asc";
        }

        return (sortBy, sortOrder);
    }

    /// <summary>
    /// Get search parameters from query string
    /// </summary>
    protected string? GetSearchParameter()
    {
        return Request.Query["search"].FirstOrDefault();
    }

    /// <summary>
    /// Get filter parameters from query string
    /// </summary>
    protected Dictionary<string, string> GetFilterParameters()
    {
        var filters = new Dictionary<string, string>();

        foreach (var query in Request.Query)
        {
            if (query.Key.StartsWith("filter."))
            {
                var filterKey = query.Key.Substring(7); // Remove "filter." prefix
                filters[filterKey] = query.Value.FirstOrDefault() ?? string.Empty;
            }
        }

        return filters;
    }
}