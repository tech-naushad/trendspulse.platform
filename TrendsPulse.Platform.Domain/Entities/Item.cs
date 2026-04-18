using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Events;
using TrendsPulse.Platform.Domain.ValueObjects;

namespace TrendsPulse.Platform.Domain.Entities;

public class Item : BaseEntity
{
    private Item() { }

    public string         Name             { get; private set; } = string.Empty;
    public string         Slug             { get; private set; } = string.Empty;
    public string?        Description      { get; private set; }
    public string?        Symbol           { get; private set; }
    public PriceUnit      Unit             { get; private set; }
    public string?        CustomUnitLabel  { get; private set; }
    public int            DecimalPrecision { get; private set; }
    public ItemStatus     Status           { get; private set; }
    public ItemVisibility Visibility       { get; private set; }
    public bool           IsSystem         { get; private set; }
    public string?        Tags             { get; private set; }
    public string?        ThumbnailUrl     { get; private set; }
    public Guid           CategoryId       { get; private set; }
    public Guid?          TenantId         { get; private set; }

    public Category                        Category           { get; private set; } = null!;
    public ICollection<DataSourceMapping>  DataSourceMappings { get; private set; } = new List<DataSourceMapping>();

    // ── Factory ─────────────────────────────────────────────────────────────
    public static Item Create(
        string name, Slug slug, string? description, string? symbol,
        PriceUnit unit, string? customUnitLabel, int decimalPrecision,
        ItemVisibility visibility, string? tags, string? thumbnailUrl,
        Guid categoryId, Guid? tenantId, string createdBy)
    {
        var item = new Item
        {
            Id               = Guid.NewGuid(),
            Name             = name.Trim(),
            Slug             = slug.Value,
            Description      = description?.Trim(),
            Symbol           = symbol?.Trim().ToUpperInvariant(),
            Unit             = unit,
            CustomUnitLabel  = customUnitLabel?.Trim(),
            DecimalPrecision = decimalPrecision,
            Status           = ItemStatus.Active,
            Visibility       = visibility,
            IsSystem         = false,
            Tags             = tags,
            ThumbnailUrl     = thumbnailUrl,
            CategoryId       = categoryId,
            TenantId         = tenantId,
            CreatedBy        = createdBy,
            CreatedAt        = DateTime.UtcNow
        };
        item.AddDomainEvent(new ItemCreatedEvent(item.Id, item.Name, item.CategoryId));
        return item;
    }

    // ── Behaviour methods ────────────────────────────────────────────────────
    public void Update(
        string name, Slug slug, string? description, string? symbol,
        PriceUnit unit, string? customUnitLabel, int decimalPrecision,
        ItemStatus status, ItemVisibility visibility,
        string? tags, string? thumbnailUrl, Guid categoryId, string updatedBy)
    {
        Name             = name.Trim();
        Slug             = slug.Value;
        Description      = description?.Trim();
        Symbol           = symbol?.Trim().ToUpperInvariant();
        Unit             = unit;
        CustomUnitLabel  = customUnitLabel?.Trim();
        DecimalPrecision = decimalPrecision;
        Status           = status;
        Visibility       = visibility;
        Tags             = tags;
        ThumbnailUrl     = thumbnailUrl;
        CategoryId       = categoryId;
        UpdatedAt        = DateTime.UtcNow;
        UpdatedBy        = updatedBy;
    }

    public void ChangeStatus(ItemStatus newStatus, string updatedBy)
    {
        var oldStatus = Status;
        Status    = newStatus;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        if (oldStatus != newStatus)
            AddDomainEvent(new ItemStatusChangedEvent(Id, oldStatus, newStatus));
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new ItemDeletedEvent(Id));
    }

    // ── Seed constructor ─────────────────────────────────────────────────────
    public static Item CreateSystem(
        string name, string slug, string? description, string? symbol,
        PriceUnit unit, int decimalPrecision, Guid categoryId) =>
        new()
        {
            Id = Guid.NewGuid(), Name = name, Slug = slug,
            Description = description, Symbol = symbol,
            Unit = unit, DecimalPrecision = decimalPrecision,
            Status = ItemStatus.Active, Visibility = ItemVisibility.Global,
            IsSystem = true, CategoryId = categoryId,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        };
}
