using Microsoft.Extensions.Logging;
using SharedLibrary.Interfaces;
using System.Text.Json;

namespace MainAPI.Infrastructure.Services.ExternalApis
{
    public class PriceService : IPriceService
    {
        private readonly ILogger<PriceService> _logger;
        private readonly HttpClient _httpClient;

        public PriceService(ILogger<PriceService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<decimal?> GetUsdPriceAsync()
        {
            try
            {
                string url = "https://api.coingecko.com/api/v3/simple/price?ids=cardano&vs_currencies=usd";
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("cardano", out var cardanoElement) &&
                    cardanoElement.TryGetProperty("usd", out var usdElement))
                {
                    return usdElement.GetDecimal();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting USD price from external API");
                throw;
            }
        }
    }
}