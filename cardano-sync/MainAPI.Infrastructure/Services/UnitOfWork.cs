using MainAPI.Core.Interfaces;
using MainAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace MainAPI.Infrastructure.Services;

/// <summary>
/// Unit of Work implementation for transaction management
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }


    public Microsoft.EntityFrameworkCore.DbContext GetDbContext()
    {
        return _context;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}