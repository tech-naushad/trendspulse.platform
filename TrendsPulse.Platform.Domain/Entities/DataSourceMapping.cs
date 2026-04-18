using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Events;

namespace TrendsPulse.Platform.Domain.Entities;

public class DataSourceMapping : BaseEntity
{
    private DataSourceMapping() { }

    public Guid           ItemId                  { get; private set; }
    public DataSourceType SourceType              { get; private set; }
    public string         ExternalIdentifier      { get; private set; } = string.Empty;
    public bool           IsPrimary               { get; private set; }
    public FetchFrequency FetchFrequency          { get; private set; }
    public bool           IsEnabled               { get; private set; }
    public string?        AdditionalConfig        { get; private set; }
    public string?        AvFunction              { get; private set; }
    public string?        AvMarket                { get; private set; }
    public string?        CustomEndpointUrl       { get; private set; }
    public string?        CustomPriceJsonPath     { get; private set; }
    public string?        CustomTimestampJsonPath { get; private set; }
    public string?        CustomHeaders           { get; private set; }
    public DateTime?      LastSuccessAt           { get; private set; }
    public DateTime?      LastAttemptAt           { get; private set; }
    public string?        LastErrorMessage        { get; private set; }
    public int            ConsecutiveFailures     { get; private set; }

    public Item Item { get; private set; } = null!;

    // ── Factory ──────────────────────────────────────────────────────────────
    public static DataSourceMapping Create(
        Guid itemId, DataSourceType sourceType, string externalIdentifier,
        bool isPrimary, FetchFrequency fetchFrequency, bool isEnabled,
        string? additionalConfig, string? avFunction, string? avMarket,
        string? customEndpointUrl, string? customPriceJsonPath,
        string? customTimestampJsonPath, string? customHeaders, string createdBy)
    {
        var m = new DataSourceMapping
        {
            Id                      = Guid.NewGuid(),
            ItemId                  = itemId,
            SourceType              = sourceType,
            ExternalIdentifier      = externalIdentifier.Trim(),
            IsPrimary               = isPrimary,
            FetchFrequency          = fetchFrequency,
            IsEnabled               = isEnabled,
            AdditionalConfig        = additionalConfig,
            AvFunction              = avFunction?.Trim(),
            AvMarket                = avMarket?.Trim().ToUpperInvariant(),
            CustomEndpointUrl       = customEndpointUrl?.Trim(),
            CustomPriceJsonPath     = customPriceJsonPath?.Trim(),
            CustomTimestampJsonPath = customTimestampJsonPath?.Trim(),
            CustomHeaders           = customHeaders,
            CreatedBy               = createdBy,
            CreatedAt               = DateTime.UtcNow
        };
        return m;
    }

    // ── Behaviour methods ────────────────────────────────────────────────────
    public void Update(
        DataSourceType sourceType, string externalIdentifier,
        bool isPrimary, FetchFrequency fetchFrequency, bool isEnabled,
        string? additionalConfig, string? avFunction, string? avMarket,
        string? customEndpointUrl, string? customPriceJsonPath,
        string? customTimestampJsonPath, string? customHeaders, string updatedBy)
    {
        SourceType              = sourceType;
        ExternalIdentifier      = externalIdentifier.Trim();
        IsPrimary               = isPrimary;
        FetchFrequency          = fetchFrequency;
        IsEnabled               = isEnabled;
        AdditionalConfig        = additionalConfig;
        AvFunction              = avFunction?.Trim();
        AvMarket                = avMarket?.Trim().ToUpperInvariant();
        CustomEndpointUrl       = customEndpointUrl?.Trim();
        CustomPriceJsonPath     = customPriceJsonPath?.Trim();
        CustomTimestampJsonPath = customTimestampJsonPath?.Trim();
        CustomHeaders           = customHeaders;
        UpdatedAt               = DateTime.UtcNow;
        UpdatedBy               = updatedBy;
    }

    public void DemoteFromPrimary(string updatedBy)
    {
        IsPrimary = false;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Toggle(string updatedBy)
    {
        IsEnabled = !IsEnabled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void RecordSuccess()
    {
        LastSuccessAt       = DateTime.UtcNow;
        LastAttemptAt       = DateTime.UtcNow;
        ConsecutiveFailures = 0;
        LastErrorMessage    = null;
        UpdatedAt           = DateTime.UtcNow;
    }

    public void RecordFailure(string errorMessage)
    {
        LastAttemptAt = DateTime.UtcNow;
        ConsecutiveFailures++;
        LastErrorMessage = errorMessage;
        UpdatedAt        = DateTime.UtcNow;

        if (ConsecutiveFailures >= 5)
            AddDomainEvent(new MappingUnhealthyEvent(Id, ItemId, errorMessage));
    }

    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
    }
}
