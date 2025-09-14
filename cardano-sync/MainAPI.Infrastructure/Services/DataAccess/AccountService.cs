using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedLibrary.Interfaces;

namespace MainAPI.Infrastructure.Services.DataAccess
{
    public class AccountService : IAccountService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<AccountService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<int?> GetTotalStakeAddressesAsync()
        {
            try
            {
                _logger.LogInformation("Getting total stake addresses count");

                using var context = await _contextFactory.CreateDbContextAsync();

                // Use EF Core to query account_list table
                var count = await context.account_list
                    .AsNoTracking()
                    .CountAsync();

                _logger.LogInformation("Total stake addresses count: {Count}", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total stake addresses count");
                throw;
            }
        }
    }
}