using MediatR;

namespace TrendsPulse.Platform.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Implements INotification so MediatR can dispatch them.
/// </summary>
public interface IDomainEvent : INotification { }
