using Microsoft.EntityFrameworkCore.Storage;
using TrendsPulse.Platform.Domain.Interfaces;
using TrendsPulse.Platform.Infrstructure.Persistence.Repositories;

namespace TrendsPulse.Platform.Infrstructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _ctx;
    private IDbContextTransaction?       _transaction;

    public ICategoryRepository           Categories         { get; }
    public IItemRepository               Items              { get; }
    public IDataSourceMappingRepository  DataSourceMappings { get; }

    public UnitOfWork(ApplicationDbContext ctx)
    {
        _ctx               = ctx;
        Categories         = new CategoryRepository(ctx);
        Items              = new ItemRepository(ctx);
        DataSourceMappings = new DataSourceMappingRepository(ctx);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _ctx.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _ctx.Dispose();
    }
}
