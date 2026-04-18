using TrendsPulse.Platform.Application.Common.Exceptions;
using TrendsPulse.Platform.Application.Common.Interfaces;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Interfaces;
using TrendsPulse.Platform.Domain.ValueObjects;

namespace TrendsPulse.Platform.Infrstructure.Services;

// ── CategoryDomainService ─────────────────────────────────────────────────────
public sealed class CategoryDomainService : ICategoryDomainService
{
    private readonly IUnitOfWork _uow;

    public CategoryDomainService(IUnitOfWork uow) => _uow = uow;

    public async Task EnsureNameIsUniqueAsync(
        string name, Guid? tenantId, Guid? excludeId, CancellationToken ct)
    {
        var existing = await _uow.Categories.GetByNameAsync(name, tenantId, ct);
        if (existing is not null && existing.Id != excludeId)
            throw new ConflictException(
                $"A category named '{name}' already exists in this scope.");
    }

    public async Task EnsureCanDeleteAsync(Guid categoryId, CancellationToken ct)
    {
        var hasItems = await _uow.Categories.HasActiveItemsAsync(categoryId, ct);
        if (hasItems)
            throw new ConflictException(
                "Cannot delete a category that still contains items. " +
                "Move or delete all items first.");
    }
}

// ── ItemDomainService ─────────────────────────────────────────────────────────
public sealed class ItemDomainService : IItemDomainService
{
    private readonly IUnitOfWork _uow;

    public ItemDomainService(IUnitOfWork uow) => _uow = uow;

    public async Task<Slug> GenerateUniqueSlugAsync(
        string name, Guid? excludeItemId, CancellationToken ct)
    {
        var baseSlug  = Slug.Create(name);
        var candidate = baseSlug;
        var counter   = 2;

        while (await _uow.Items.SlugExistsAsync(candidate.Value, excludeItemId, ct))
        {
            candidate = baseSlug.WithSuffix(counter);
            counter++;
        }

        return candidate;
    }

    public async Task EnsureCategoryExistsAsync(
        Guid categoryId, Guid? tenantId, CancellationToken ct)
    {
        var category = await _uow.Categories.GetByIdAsync(categoryId, ct);

        if (category is null)
            throw new NotFoundException(nameof(Category), categoryId);

        // Tenant-private categories are only accessible to their owner
        if (category.TenantId.HasValue && category.TenantId != tenantId)
            throw new NotFoundException(nameof(Category), categoryId);
    }
}
