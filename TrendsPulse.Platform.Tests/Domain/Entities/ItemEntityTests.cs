using FluentAssertions;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Events;
using TrendsPulse.Platform.Domain.ValueObjects;
using Xunit;

namespace TrendsPulse.Platform.Domain.Tests.Tests.Entities;

public class ItemEntityTests
{
    private static Item BuildItem(
        string name = "Gold Spot",
        Guid? tenantId = null,
        ItemVisibility visibility = ItemVisibility.Tenant)
    {
        var slug = Slug.Create(name);
        return Item.Create(
            name, slug, "desc", "XAU", PriceUnit.UsdPerOunce,
            null, 2, visibility, null, null,
            Guid.NewGuid(), tenantId, "seed");
    }

    [Fact]
    public void Create_ShouldDefaultStatusToActive()
    {
        var item = BuildItem();
        item.Status.Should().Be(ItemStatus.Active);
    }

    [Fact]
    public void Create_ShouldUppercaseSymbol()
    {
        var slug = Slug.Create("test");
        var item = Item.Create("Test", slug, null, "xau",
            PriceUnit.UsdPerOunce, null, 2, ItemVisibility.Tenant,
            null, null, Guid.NewGuid(), null, "user");
        item.Symbol.Should().Be("XAU");
    }

    [Fact]
    public void Create_ShouldRaiseItemCreatedEvent()
    {
        var item = BuildItem();
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ItemCreatedEvent>();
    }

    [Fact]
    public void ChangeStatus_ShouldRaiseStatusChangedEvent()
    {
        var item = BuildItem();
        item.ClearDomainEvents();

        item.ChangeStatus(ItemStatus.Paused, "admin");

        item.Status.Should().Be(ItemStatus.Paused);
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ItemStatusChangedEvent>()
            .Which.NewStatus.Should().Be(ItemStatus.Paused);
    }

    [Fact]
    public void ChangeStatus_ShouldNotRaiseEvent_WhenStatusUnchanged()
    {
        var item = BuildItem();
        item.ClearDomainEvents();

        item.ChangeStatus(ItemStatus.Active, "admin"); // already active

        item.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedAndRaiseEvent()
    {
        var item = BuildItem();
        item.ClearDomainEvents();

        item.SoftDelete("admin");

        item.IsDeleted.Should().BeTrue();
        item.DeletedAt.Should().NotBeNull();
        item.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ItemDeletedEvent>();
    }
}
