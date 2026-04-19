using FluentAssertions;
using TrendsPulse.Platform.Domain.Entities;
using TrendsPulse.Platform.Domain.Enums;
using TrendsPulse.Platform.Domain.Events;
using Xunit;

namespace TrendsPulse.Platform.Domain.Tests.Tests.Entities;

public class CategoryEntityTests
{
    [Fact]
    public void Create_ShouldSetAllPropertiesCorrectly()
    {
        var tenantId = Guid.NewGuid();

        var cat = Category.Create(
            "  My Category  ", "A description", "gem",
            "#F59E0B", CategoryType.Metals, 10, tenantId, "test-user");

        cat.Name.Should().Be("My Category");   // trimmed
        cat.Description.Should().Be("A description");
        cat.IconCode.Should().Be("gem");
        cat.ColorHex.Should().Be("#F59E0B");
        cat.Type.Should().Be(CategoryType.Metals);
        cat.DisplayOrder.Should().Be(10);
        cat.TenantId.Should().Be(tenantId);
        cat.IsSystem.Should().BeFalse();
        cat.IsDeleted.Should().BeFalse();
        cat.CreatedBy.Should().Be("test-user");
    }

    [Fact]
    public void Create_ShouldRaiseCategoryCreatedEvent()
    {
        var cat = Category.Create(
            "Gold", null, null, null, CategoryType.Metals, 10, null, "user");

        cat.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryCreatedEvent>()
            .Which.Name.Should().Be("Gold");
    }

    [Fact]
    public void Update_ShouldModifyFieldsAndSetUpdatedAt()
    {
        var cat = Category.Create(
            "Old Name", null, null, null, CategoryType.Metals, 10, null, "user");
        cat.ClearDomainEvents();

        var before = DateTime.UtcNow.AddSeconds(-1);
        cat.Update("New Name", "Desc", "icon", "#fff",
            CategoryType.Crypto, 20, "editor");

        cat.Name.Should().Be("New Name");
        cat.DisplayOrder.Should().Be(20);
        cat.UpdatedBy.Should().Be("editor");
        cat.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedAndRaiseEvent()
    {
        var cat = Category.Create(
            "DeleteMe", null, null, null, CategoryType.Custom, 10, null, "user");
        cat.ClearDomainEvents();

        cat.SoftDelete("admin");

        cat.IsDeleted.Should().BeTrue();
        cat.DeletedAt.Should().NotBeNull();
        cat.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryDeletedEvent>();
    }
}
