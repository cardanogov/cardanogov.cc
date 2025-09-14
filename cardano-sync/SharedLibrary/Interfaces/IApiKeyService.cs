using SharedLibrary.Models;

namespace SharedLibrary.Interfaces
{
    public interface IApiKeyService
    {
        Task<ApiKey?> GetApiKeyAsync(string key);
        Task<bool> ValidateApiKeyAsync(string key);
        Task<RateLimitInfo> CheckRateLimitAsync(string key, string endpoint);
        Task IncrementRequestCountAsync(string key);
        Task<bool> IsRateLimitExceededAsync(string key, string endpoint);
        Task<ApiKey?> CreateApiKeyAsync(string name, string description, ApiKeyType type, string? createdBy = null);
        Task<bool> DeactivateApiKeyAsync(string key);
        Task<List<ApiKey>> GetAllApiKeysAsync();
        Task<bool> UpdateApiKeyAsync(ApiKey apiKey);

        // Anonymous/Free session methods
        Task<RateLimitInfo> CheckAnonymousRateLimitAsync(string ipAddress, string endpoint);
        Task IncrementAnonymousRequestCountAsync(string ipAddress);
    }
}