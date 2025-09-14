//using Microsoft.Extensions.Logging;
//using SharedLibrary.Services;

//namespace MainAPI.Infrastructure.Services.ExternalApis
//{
//    public class ImageService : IImageService
//    {
//        private readonly ILogger<ImageService> _logger;
//        private readonly HttpClient _httpClient;

//        public ImageService(ILogger<ImageService> logger, HttpClient httpClient)
//        {
//            _logger = logger;
//            _httpClient = httpClient;
//        }

//        public async Task<object?> GetImageAsync()
//        {
//            try
//            {
//                // TODO: Implement external API call to get image
//                await Task.Delay(1);
//                return null;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting image from external API");
//                throw;
//            }
//        }
//    }
//}