namespace SharedLibrary.Interfaces
{
    public interface IAccountService
    {
        Task<int?> GetTotalStakeAddressesAsync();
    }
}