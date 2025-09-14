using MainAPI.Application.Common.Constants;
using MainAPI.Core;
using MainAPI.Core.Interfaces;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedLibrary.Interfaces;
using SharedLibrary.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class ImageService : IImageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly ApplicationDbContext _dbContext;

        private const string DEEPAI_API_URL = "https://api.deepai.org/api/text2img";
        private const string InstanceCache = "CardanoMainAPI:";

        public ImageService(IConfiguration configuration, ILogger<ImageService> logger, HttpClient httpClient, ICacheService cacheService, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
            _cacheService = cacheService;
            _dbContext = dbContext;
        }

        public async Task<string?> GetImageAsync(string text, string subtext)
        {
            try
            {
                _logger.LogInformation("Executing GetImageQuery for text: {text}, subtext: {subtext}", text, subtext);

                // Create cache key based on text and subtext
                var cacheKey = $"{CacheKeys.IMAGE_GENERATION}_{ReplaceSpace(text)}";

                // Step 1: Try to get from cache first
                var cachedImageResult = await _cacheService.GetAsync<string>(cacheKey);
                if (cachedImageResult.IsHit && !string.IsNullOrEmpty(cachedImageResult.Value))
                {
                    _logger.LogInformation("Image found in cache: {ImageUrl}", cachedImageResult.Value);
                    return cachedImageResult.Value;
                }

                // Step 2: Try to get from database
                var dbImage = await GetImageFromDatabase(text, subtext, InstanceCache + cacheKey);
                if (!string.IsNullOrEmpty(dbImage))
                {
                    _logger.LogInformation("Image found in database, adding to cache: {ImageUrl}", dbImage);
                    // Add to cache for future requests
                    await _cacheService.SetAsync(cacheKey, dbImage, TimeUtils.GetSecondsFromYears(1));
                    return dbImage;
                }

                // Step 3: Generate new image from DeepAI API
                _logger.LogInformation("Image not found in cache or database, generating from DeepAI API");
                var newImageUrl = await GenerateImageFromDeepAI(text, subtext, new CancellationToken());

                if (!string.IsNullOrEmpty(newImageUrl) && newImageUrl != "/assets/icons/cardano.png")
                {
                    // Save to database
                    await SaveImageToDatabase(text, subtext, newImageUrl, InstanceCache + cacheKey);

                    // Save to cache
                    await _cacheService.SetAsync(cacheKey, newImageUrl, TimeUtils.GetSecondsFromYears(1));

                    _logger.LogInformation("Generated and saved new image: {ImageUrl}", newImageUrl);
                    return newImageUrl;
                }

                _logger.LogWarning("Failed to generate image, returning default");
                return "/assets/icons/cardano.png";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetImageQuery");
                return "/assets/icons/cardano.png"; // Return default image on error
            }
        }

        private async Task<string> GenerateImageFromDeepAI(string text, string subtext, CancellationToken cancellationToken)
        {
            try
            {
                var requestBody = new
                {
                    text = $"Decentralized Governance on Cardano Blockchain, Governance Actions: {text} {subtext}",
                    image_generator_version = "genius",
                    turbo = "true",
                    genius_preference = "graphic"
                };

                return "/assets/icons/cardano.png";

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Add API key header
                var apiKey = _configuration["DeepAIKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("DEEPAI_API_KEY environment variable not found");
                    return "/assets/icons/cardano.png";
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/text2img")
                {
                    Content = content
                };
                request.Headers.Add("Api-Key", apiKey);

                _logger.LogInformation("Calling DeepAI API: {Url}", DEEPAI_API_URL);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("DeepAI API returned {StatusCode}: {ErrorContent}", response.StatusCode, errorContent);
                    return "/assets/icons/cardano.png";
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = System.Text.Json.JsonSerializer.Deserialize<DeepAIResponse>(responseContent);

                if (result?.ShareUrl != null)
                {
                    _logger.LogInformation("Successfully generated image: {ShareUrl}", result.ShareUrl);
                    return result.ShareUrl;
                }
                else
                {
                    _logger.LogWarning("No share_url in DeepAI response");
                    return "/assets/icons/cardano.png";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image from DeepAI API");
                return "/assets/icons/cardano.png";
            }
        }

        private async Task<string?> GetImageFromDatabase(string text, string subtext, string cacheKey)
        {
            try
            {
                var existingImage = await _dbContext.images
                    .Where(img => img.Text == text && img.Subtext == subtext || img.CacheKey.ToLower().Contains(cacheKey.ToLower()))
                    .OrderByDescending(img => img.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingImage != null)
                {
                    _logger.LogInformation("Found existing image in database for text: {text}, subtext: {subtext}", text, subtext);
                    return existingImage.ImageUrl;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image from database for text: {text}, subtext: {subtext}", text, subtext);
                return null;
            }
        }

        private async Task SaveImageToDatabase(string text, string subtext, string imageUrl, string cacheKey)
        {
            try
            {
                var generatedImage = new GeneratedImage
                {
                    Text = text,
                    Subtext = subtext,
                    ImageUrl = imageUrl,
                    CacheKey = cacheKey,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.images.Add(generatedImage);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully saved image to database: {ImageUrl}", imageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving image to database for text: {text}, subtext: {subtext}", text, subtext);
                // Don't throw - we don't want to fail the entire request if database save fails
            }
        }

        private static string ReplaceSpace(string input)
        {
            return input?.Replace(" ", "_") ?? string.Empty;
        }
    }

    internal class DeepAIResponse
    {
        [JsonPropertyName("share_url")]
        public string? ShareUrl { get; set; }
    }
}
