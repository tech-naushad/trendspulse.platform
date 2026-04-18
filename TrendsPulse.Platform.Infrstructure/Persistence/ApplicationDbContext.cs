using MediatR;
using Microsoft.EntityFrameworkCore;
using TrendsPulse.Platform.Domain.Common;
using TrendsPulse.Platform.Domain.Entities;

namespace TrendsPulse.Platform.Infrstructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly IMediator _mediator;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMediator mediator) : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Category>          Categories          => Set<Category>();
    public DbSet<Item>              Items               => Set<Item>();
    public DbSet<DataSourceMapping> DataSourceMappings  => Set<DataSourceMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Auto-set UpdatedAt
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
            entry.Entity.UpdatedAt = DateTime.UtcNow;

        var result = await base.SaveChangesAsync(ct);

        // Dispatch domain events AFTER saving
        await DispatchDomainEventsAsync(ct);

        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var @event in events)
            await _mediator.Publish(@event, ct);
    }
}
