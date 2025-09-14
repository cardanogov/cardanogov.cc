namespace SharedLibrary.Interfaces
{
    public interface IPriceService
    {
        Task<decimal?> GetUsdPriceAsync();
    }
}