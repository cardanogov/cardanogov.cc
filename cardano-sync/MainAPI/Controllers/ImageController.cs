using MainAPI.Models;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Interfaces;

namespace MainAPI.Controllers
{
    [ApiController]
    [Route("api/")]
    public class ImageController : BaseController
    {
        private readonly IImageService _imageService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ILogger<ImageController> logger, IImageService imageService) : base(logger)
        {
            _logger = logger;
            _imageService = imageService;
        }

        [HttpGet("image")]
        public async Task<IActionResult> GetImage([FromQuery] string text, [FromQuery] string subtext = "")
        {
            try
            {
                _logger.LogInformation("Getting image for text: {Text}, subtext: {Subtext}", text, subtext);

                if (string.IsNullOrEmpty(text))
                {
                    return BadRequest(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Text parameter is required"
                    });
                }

                var imageUrl = await _imageService.GetImageAsync(text, subtext);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Data = imageUrl,
                    Message = "Image generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving image");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Internal server error while generating image"
                });
            }
        }
    }
}