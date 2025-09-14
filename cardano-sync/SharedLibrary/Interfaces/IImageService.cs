namespace SharedLibrary.Interfaces
{
    public interface IImageService
    {
        Task<string?> GetImageAsync(string text, string subtext);
    }
}