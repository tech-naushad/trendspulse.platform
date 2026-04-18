using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;

namespace TrendsPulse.Platform.Application.Common.Mappings;

// ── Category ──────────────────────────────────────────────────────────────────
public record CategoryDto(
    Guid    Id,
    string  Name,
    string? Description,
    string? IconCode,
    string? ColorHex,
    CategoryType Type,
    string  TypeLabel,
    int     DisplayOrder,
    bool    IsSystem,
    Guid?   TenantId,
    int     ItemCount,
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

public record CategorySummaryDto(
    Guid   Id,
    string Name,
    string? IconCode,
    string? ColorHex,
    CategoryType Type,
    string TypeLabel,
    int    ItemCount
);

// ── Item ──────────────────────────────────────────────────────────────────────
public record ItemDto(
    Guid   Id,
    string Name,
    string Slug,
    string? Description,
    string? Symbol,
    PriceUnit Unit,
    string  UnitLabel,
    string? CustomUnitLabel,
    int     DecimalPrecision,
    ItemStatus    Status,
    string        StatusLabel,
    ItemVisibility Visibility,
    bool   IsSystem,
    string? Tags,
    string? ThumbnailUrl,
    Guid    CategoryId,
    CategorySummaryDto Category,
    Guid?   TenantId,
    int     MappingCount,
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

public record ItemSummaryDto(
    Guid   Id,
    string Name,
    string Slug,
    string? Symbol,
    PriceUnit Unit,
    string  UnitLabel,
    ItemStatus Status,
    Guid    CategoryId,
    string  CategoryName,
    string? CategoryIconCode
);

// ── DataSourceMapping ─────────────────────────────────────────────────────────
public record DataSourceMappingDto(
    Guid   Id,
    Guid   ItemId,
    string ItemName,
    DataSourceType SourceType,
    string SourceTypeLabel,
    string ExternalIdentifier,
    bool   IsPrimary,
    FetchFrequency FetchFrequency,
    string FetchFrequencyLabel,
    bool   IsEnabled,
    string? AdditionalConfig,
    string? AvFunction,
    string? AvMarket,
    string? CustomEndpointUrl,
    string? CustomPriceJsonPath,
    string? CustomTimestampJsonPath,
    string? CustomHeaders,
    DateTime? LastSuccessAt,
    DateTime? LastAttemptAt,
    string? LastErrorMessage,
    int     ConsecutiveFailures,
    string  HealthStatus,
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

// ── Static mappers (no AutoMapper dependency) ─────────────────────────────────
public static class CategoryMapper
{
    public static CategoryDto ToDto(Category c, int itemCount) => new(
        c.Id, c.Name, c.Description, c.IconCode, c.ColorHex,
        c.Type, c.Type.ToString(), c.DisplayOrder, c.IsSystem,
        c.TenantId, itemCount, c.CreatedAt, c.UpdatedAt);

    public static CategorySummaryDto ToSummaryDto(Category c, int itemCount) => new(
        c.Id, c.Name, c.IconCode, c.ColorHex, c.Type, c.Type.ToString(), itemCount);
}

public static class ItemMapper
{
    public static ItemDto ToDto(Item i) => new(
        i.Id, i.Name, i.Slug, i.Description, i.Symbol, i.Unit,
        i.CustomUnitLabel ?? i.Unit.ToString(), i.CustomUnitLabel,
        i.DecimalPrecision, i.Status, i.Status.ToString(), i.Visibility,
        i.IsSystem, i.Tags, i.ThumbnailUrl, i.CategoryId,
        CategoryMapper.ToSummaryDto(i.Category,
            i.Category.Items.Count(x => !x.IsDeleted)),
        i.TenantId,
        i.DataSourceMappings.Count(m => !m.IsDeleted),
        i.CreatedAt, i.UpdatedAt);

    public static ItemSummaryDto ToSummaryDto(Item i) => new(
        i.Id, i.Name, i.Slug, i.Symbol, i.Unit,
        i.CustomUnitLabel ?? i.Unit.ToString(), i.Status,
        i.CategoryId, i.Category?.Name ?? "", i.Category?.IconCode);
}

public static class DataSourceMappingMapper
{
    public static DataSourceMappingDto ToDto(
        Domain.Entities.DataSourceMapping m, string itemName) => new(
        m.Id, m.ItemId, itemName, m.SourceType, m.SourceType.ToString(),
        m.ExternalIdentifier, m.IsPrimary, m.FetchFrequency,
        m.FetchFrequency.ToString(), m.IsEnabled, m.AdditionalConfig,
        m.AvFunction, m.AvMarket, m.CustomEndpointUrl,
        m.CustomPriceJsonPath, m.CustomTimestampJsonPath, m.CustomHeaders,
        m.LastSuccessAt, m.LastAttemptAt, m.LastErrorMessage,
        m.ConsecutiveFailures, ComputeHealth(m),
        m.CreatedAt, m.UpdatedAt);

    private static string ComputeHealth(Domain.Entities.DataSourceMapping m)
    {
        if (!m.IsEnabled)                                          return "Disabled";
        if (m.ConsecutiveFailures >= 5)                           return "Unhealthy";
        if (m.ConsecutiveFailures >= 2)                           return "Degraded";
        if (m.LastSuccessAt is null)                              return "NeverFetched";
        if (m.LastSuccessAt < DateTime.UtcNow.AddHours(-6))      return "Stale";
        return "Healthy";
    }
}
