using FluentAssertions;
using TrendsPulse.Platform.Domain.ValueObjects;
using Xunit;

namespace TrendsPulse.Platform.Domain.Tests.Tests.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("Gold Spot",          "gold-spot")]
    [InlineData("Crude Oil (WTI)",    "crude-oil-wti")]
    [InlineData("EUR/USD",            "eur-usd")]
    [InlineData("  Leading spaces  ", "leading-spaces")]
    [InlineData("Café Prix",          "cafe-prix")]
    [InlineData("Multiple---hyphens", "multiple-hyphens")]
    [InlineData("UPPERCASE NAME",     "uppercase-name")]
    public void Create_ShouldProduceLowercaseHyphenatedSlug(string input, string expected)
    {
        var slug = Slug.Create(input);
        slug.Value.Should().Be(expected);
    }

    [Fact]
    public void Create_ShouldThrow_WhenInputIsEmpty()
    {
        var act = () => Slug.Create("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldThrow_WhenOnlySpecialChars()
    {
        var act = () => Slug.Create("!@#$%^");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTruncateTo200Chars()
    {
        var longInput = new string('a', 300);
        var slug = Slug.Create(longInput);
        slug.Value.Length.Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public void WithSuffix_ShouldAppendNumber()
    {
        var slug = Slug.Create("gold-spot");
        var suffixed = slug.WithSuffix(2);
        suffixed.Value.Should().Be("gold-spot-2");
    }

    [Fact]
    public void Equality_ShouldBeValueBased()
    {
        var a = Slug.Create("gold-spot");
        var b = Slug.Create("Gold Spot");
        a.Should().Be(b);
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnValue()
    {
        var slug = Slug.Create("bitcoin");
        string s = slug;
        s.Should().Be("bitcoin");
    }
}
