using System.Linq.Expressions;
using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;

namespace TrendsPulse.Platform.Domain.Interfaces;

// ── Generic ───────────────────────────────────────────────────────────────────
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}

// ── Category ──────────────────────────────────────────────────────────────────
public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByNameAsync(string name, Guid? tenantId, CancellationToken ct = default);
    Task<IEnumerable<Category>> GetVisibleToTenantAsync(Guid? tenantId, CancellationToken ct = default);
    Task<bool> HasActiveItemsAsync(Guid categoryId, CancellationToken ct = default);
    Task<int> GetNextDisplayOrderAsync(Guid? tenantId, CancellationToken ct = default);
}

// ── Item ──────────────────────────────────────────────────────────────────────
public interface IItemRepository : IRepository<Item>
{
    Task<Item?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Item?> GetWithMappingsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Item> Items, int TotalCount)> GetPagedAsync(
        Guid? tenantId, Guid? categoryId, ItemStatus? status,
        string? search, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Item>> GetByCategoryAsync(Guid categoryId, Guid? tenantId, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default);
}

// ── DataSourceMapping ─────────────────────────────────────────────────────────
public interface IDataSourceMappingRepository : IRepository<DataSourceMapping>
{
    Task<IEnumerable<DataSourceMapping>> GetByItemAsync(Guid itemId, CancellationToken ct = default);
    Task<DataSourceMapping?> GetPrimaryForItemAsync(Guid itemId, CancellationToken ct = default);
    Task<IEnumerable<DataSourceMapping>> GetActiveForIngestionAsync(CancellationToken ct = default);
}

// ── Unit of Work ──────────────────────────────────────────────────────────────
public interface IUnitOfWork : IDisposable
{
    ICategoryRepository        Categories         { get; }
    IItemRepository            Items              { get; }
    IDataSourceMappingRepository DataSourceMappings { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
