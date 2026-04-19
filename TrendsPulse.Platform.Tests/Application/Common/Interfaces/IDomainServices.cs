
using TrendsPulse.Platform.Domain.ValueObjects;

namespace TrendsPulse.Platform.Application.Tests.Common.Interfaces;

/// <summary>
/// Encapsulates category business rules so handlers stay thin
/// and rules are independently unit testable.
/// </summary>
public interface ICategoryDomainService
{
    /// <summary>Validates name uniqueness within tenant scope.</summary>
    Task EnsureNameIsUniqueAsync(string name, Guid? tenantId, Guid? excludeId, CancellationToken ct);

    /// <summary>Guards deletion — throws if category has active items.</summary>
    Task EnsureCanDeleteAsync(Guid categoryId, CancellationToken ct);
}

/// <summary>
/// Encapsulates item business rules.
/// </summary>
public interface IItemDomainService
{
    /// <summary>Generates a slug and guarantees its uniqueness.</summary>
    Task<Slug> GenerateUniqueSlugAsync(string name, Guid? excludeItemId, CancellationToken ct);

    /// <summary>Validates category exists and is accessible to tenant.</summary>
    Task EnsureCategoryExistsAsync(Guid categoryId, Guid? tenantId, CancellationToken ct);
}
