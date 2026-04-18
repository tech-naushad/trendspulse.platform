namespace TrendsPulse.Platform.Domain.Common;

/// <summary>
/// Base for all persisted entities.
/// Carries audit fields and a domain event collection.
/// Domain events are dispatched by the UnitOfWork after SaveChanges.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string?  CreatedBy  { get; set; }
    public string?  UpdatedBy  { get; set; }
    public bool     IsDeleted  { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
