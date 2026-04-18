using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Enums;

namespace TrendsPulse.Platform.Domain.Events;

// ── Category events ───────────────────────────────────────────────────────────
public sealed record CategoryCreatedEvent(Guid CategoryId, string Name) : IDomainEvent;
public sealed record CategoryDeletedEvent(Guid CategoryId) : IDomainEvent;

// ── Item events ───────────────────────────────────────────────────────────────
public sealed record ItemCreatedEvent(Guid ItemId, string Name, Guid CategoryId) : IDomainEvent;
public sealed record ItemStatusChangedEvent(Guid ItemId, ItemStatus OldStatus, ItemStatus NewStatus) : IDomainEvent;
public sealed record ItemDeletedEvent(Guid ItemId) : IDomainEvent;

// ── Mapping events ────────────────────────────────────────────────────────────
public sealed record MappingUnhealthyEvent(Guid MappingId, Guid ItemId, string LastError) : IDomainEvent;
