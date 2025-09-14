namespace MainAPI.Core.Interfaces;

/// <summary>
/// Unit of Work interface for transaction management
/// </summary>
public interface IUnitOfWork : IDisposable
{    /// <summary>
     /// Get DbContext for raw SQL queries
     /// </summary>
    Microsoft.EntityFrameworkCore.DbContext GetDbContext();
}