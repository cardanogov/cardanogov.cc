using MainAPI.Models;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class ApiKeyController : BaseController
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeyController> _logger;

        public ApiKeyController(IApiKeyService apiKeyService, ILogger<ApiKeyController> logger) : base(logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<ApiKey>>> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Name))
                {
                    return ValidationError<ApiKey>(new List<string> { "Name is required" });
                }

                var apiKey = await _apiKeyService.CreateApiKeyAsync(
                    request.Name,
                    request.Description,
                    request.Type,
                    request.CreatedBy);

                if (apiKey == null)
                {
                    return Error<ApiKey>("Failed to create API key", statusCode: 500);
                }

                return Success(apiKey, "API key created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return Error<ApiKey>($"Error creating API key: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<List<ApiKey>>>> GetAllApiKeys()
        {
            try
            {
                var apiKeys = await _apiKeyService.GetAllApiKeysAsync();
                return Success(apiKeys, "API keys retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all API keys");
                return Error<List<ApiKey>>($"Error getting API keys: {ex.Message}", statusCode: 500);
            }
        }

        [HttpPost("deactivate/{key}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeactivateApiKey(string key)
        {
            try
            {
                var result = await _apiKeyService.DeactivateApiKeyAsync(key);
                if (!result)
                {
                    return Error<bool>("API key not found or already deactivated", statusCode: 404);
                }

                return Success(true, "API key deactivated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating API key: {Key}", key);
                return Error<bool>($"Error deactivating API key: {ex.Message}", statusCode: 500);
            }
        }

        [HttpPut("update")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateApiKey([FromBody] UpdateApiKeyRequest request)
        {
            try
            {
                var apiKey = await _apiKeyService.GetApiKeyAsync(request.Key);
                if (apiKey == null)
                {
                    return Error<bool>("API key not found", statusCode: 404);
                }

                // Update properties
                apiKey.Name = request.Name ?? apiKey.Name;
                apiKey.Description = request.Description ?? apiKey.Description;
                apiKey.Type = request.Type;
                apiKey.IsActive = request.IsActive;
                apiKey.ExpiresAt = request.ExpiresAt;
                apiKey.AllowedOrigins = request.AllowedOrigins;
                apiKey.AllowedEndpoints = request.AllowedEndpoints;

                var result = await _apiKeyService.UpdateApiKeyAsync(apiKey);
                if (!result)
                {
                    return Error<bool>("Failed to update API key", statusCode: 500);
                }

                return Success(true, "API key updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key: {Key}", request.Key);
                return Error<bool>($"Error updating API key: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("validate/{key}")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateApiKey(string key)
        {
            try
            {
                var isValid = await _apiKeyService.ValidateApiKeyAsync(key);
                return Success(isValid, isValid ? "API key is valid" : "API key is invalid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key: {Key}", key);
                return Error<bool>($"Error validating API key: {ex.Message}", statusCode: 500);
            }
        }

        [HttpGet("rate-limit/{key}")]
        public async Task<ActionResult<ApiResponse<RateLimitInfo>>> GetRateLimitInfo(string key, [FromQuery] string endpoint = "")
        {
            try
            {
                var rateLimitInfo = await _apiKeyService.CheckRateLimitAsync(key, endpoint);
                return Success(rateLimitInfo, "Rate limit info retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rate limit info for key: {Key}", key);
                return Error<RateLimitInfo>($"Error getting rate limit info: {ex.Message}", statusCode: 500);
            }
        }
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ApiKeyType Type { get; set; } = ApiKeyType.Free;
        public string? CreatedBy { get; set; }
    }

    public class UpdateApiKeyRequest
    {
        public string Key { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public ApiKeyType Type { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public string? AllowedOrigins { get; set; }
        public string? AllowedEndpoints { get; set; }
    }
}