using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Interfaces;
using TrendsPulse.Platform.Infrstructure.Persistence;

namespace TrendsPulse.Platform.Infrstructure.Persistence.Repositories;

// ── Generic base ──────────────────────────────────────────────────────────────
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _ctx;
    protected readonly DbSet<T>            _set;

    public Repository(ApplicationDbContext ctx)
    {
        _ctx = ctx;
        _set = ctx.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.Where(predicate).ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity)
        => _set.Update(entity);

    public async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _set.AnyAsync(predicate, ct);
}

// ── CategoryRepository ────────────────────────────────────────────────────────
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<Category?> GetByNameAsync(
        string name, Guid? tenantId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(
            c => c.Name.ToLower() == name.ToLower().Trim()
              && c.TenantId == tenantId, ct);

    public async Task<IEnumerable<Category>> GetVisibleToTenantAsync(
        Guid? tenantId, CancellationToken ct = default)
        => await _set
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .Where(c => c.TenantId == null || c.TenantId == tenantId)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<bool> HasActiveItemsAsync(
        Guid categoryId, CancellationToken ct = default)
        => await _ctx.Items.AnyAsync(
            i => i.CategoryId == categoryId && !i.IsDeleted, ct);

    public async Task<int> GetNextDisplayOrderAsync(
        Guid? tenantId, CancellationToken ct = default)
    {
        var max = await _set
            .Where(c => c.TenantId == tenantId)
            .MaxAsync(c => (int?)c.DisplayOrder, ct);
        return (max ?? 0) + 10;
    }
}

// ── ItemRepository ────────────────────────────────────────────────────────────
public class ItemRepository : Repository<Item>, IItemRepository
{
    public ItemRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<Item?> GetBySlugAsync(
        string slug, CancellationToken ct = default)
        => await _set
            .Include(i => i.Category)
            .Include(i => i.DataSourceMappings.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(i => i.Slug == slug, ct);

    public async Task<Item?> GetWithMappingsAsync(
        Guid id, CancellationToken ct = default)
        => await _set
            .Include(i => i.Category)
                .ThenInclude(c => c.Items.Where(ci => !ci.IsDeleted))
            .Include(i => i.DataSourceMappings.Where(m => !m.IsDeleted))
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<(IEnumerable<Item> Items, int TotalCount)> GetPagedAsync(
        Guid? tenantId, Guid? categoryId, ItemStatus? status,
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _set.Include(i => i.Category).AsQueryable();

        query = query.Where(i =>
            i.TenantId == null ||
            i.TenantId == tenantId ||
            i.Visibility == ItemVisibility.Global);

        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(i => i.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(term) ||
                (i.Symbol != null && i.Symbol.ToLower().Contains(term)) ||
                (i.Description != null && i.Description.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.Category.DisplayOrder)
            .ThenBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<Item>> GetByCategoryAsync(
        Guid categoryId, Guid? tenantId, CancellationToken ct = default)
        => await _set
            .Include(i => i.Category)
            .Where(i => i.CategoryId == categoryId
                && (i.TenantId == null || i.TenantId == tenantId
                    || i.Visibility == ItemVisibility.Global))
            .OrderBy(i => i.Name)
            .ToListAsync(ct);

    public async Task<bool> SlugExistsAsync(
        string slug, Guid? excludeId, CancellationToken ct = default)
    {
        var query = _set.IgnoreQueryFilters().Where(i => i.Slug == slug);
        if (excludeId.HasValue)
            query = query.Where(i => i.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }
}

// ── DataSourceMappingRepository ───────────────────────────────────────────────
public class DataSourceMappingRepository
    : Repository<DataSourceMapping>, IDataSourceMappingRepository
{
    public DataSourceMappingRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<IEnumerable<DataSourceMapping>> GetByItemAsync(
        Guid itemId, CancellationToken ct = default)
        => await _set
            .Where(m => m.ItemId == itemId)
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.SourceType)
            .ToListAsync(ct);

    public async Task<DataSourceMapping?> GetPrimaryForItemAsync(
        Guid itemId, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(
            m => m.ItemId == itemId && m.IsPrimary, ct);

    public async Task<IEnumerable<DataSourceMapping>> GetActiveForIngestionAsync(
        CancellationToken ct = default)
        => await _set
            .Include(m => m.Item)
            .Where(m => m.IsEnabled && m.Item.Status == ItemStatus.Active)
            .OrderBy(m => m.SourceType)
            .ToListAsync(ct);
}
