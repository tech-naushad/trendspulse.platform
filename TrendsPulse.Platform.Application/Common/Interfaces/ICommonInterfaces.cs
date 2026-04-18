namespace TrendsPulse.Platform.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid    UserId        { get; }
    Guid?   TenantId      { get; }
    string  UserName      { get; }
    bool    IsAuthenticated { get; }
    bool    IsSuperAdmin  { get; }
    bool    IsTenantAdmin { get; }
}

public interface ISlugUniquenessChecker
{
    Task<bool> ExistsAsync(string slug, Guid? excludeItemId, CancellationToken ct = default);
}

/// <summary>
/// Implement on a Query record to opt it into CachingBehaviour.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey        { get; }
    TimeSpan CacheDuration { get; }
}
