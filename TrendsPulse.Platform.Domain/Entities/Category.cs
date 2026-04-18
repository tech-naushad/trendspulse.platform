using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Events;

namespace TrendsPulse.Platform.Domain.Entities;

public class Category : BaseEntity
{
    // EF Core requires parameterless constructor
    private Category() { }

    public string       Name         { get; private set; } = string.Empty;
    public string?      Description  { get; private set; }
    public string?      IconCode     { get; private set; }
    public string?      ColorHex     { get; private set; }
    public CategoryType Type         { get; private set; }
    public int          DisplayOrder { get; private set; }
    public bool         IsSystem     { get; private set; }
    public Guid?        TenantId     { get; private set; }

    public ICollection<Item> Items { get; private set; } = new List<Item>();

    // ── Factory (enforces invariants, raises domain event) ──────────────────
    public static Category Create(
        string name, string? description, string? iconCode,
        string? colorHex, CategoryType type, int displayOrder,
        Guid? tenantId, string createdBy)
    {
        var category = new Category
        {
            Id           = Guid.NewGuid(),
            Name         = name.Trim(),
            Description  = description?.Trim(),
            IconCode     = iconCode,
            ColorHex     = colorHex,
            Type         = type,
            DisplayOrder = displayOrder,
            IsSystem     = false,
            TenantId     = tenantId,
            CreatedBy    = createdBy,
            CreatedAt    = DateTime.UtcNow
        };
        category.AddDomainEvent(new CategoryCreatedEvent(category.Id, category.Name));
        return category;
    }

    // ── Behaviour methods ───────────────────────────────────────────────────
    public void Update(string name, string? description, string? iconCode,
        string? colorHex, CategoryType type, int displayOrder, string updatedBy)
    {
        Name         = name.Trim();
        Description  = description?.Trim();
        IconCode     = iconCode;
        ColorHex     = colorHex;
        Type         = type;
        DisplayOrder = displayOrder;
        UpdatedAt    = DateTime.UtcNow;
        UpdatedBy    = updatedBy;
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CategoryDeletedEvent(Id));
    }

    // ── Seed constructor (for DatabaseSeeder only) ──────────────────────────
    public static Category CreateSystem(
        Guid id, string name, CategoryType type,
        string iconCode, string colorHex, int displayOrder) =>
        new()
        {
            Id = id, Name = name, Type = type,
            IconCode = iconCode, ColorHex = colorHex,
            DisplayOrder = displayOrder, IsSystem = true,
            CreatedBy = "seed", CreatedAt = DateTime.UtcNow
        };
}
