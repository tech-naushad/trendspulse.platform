using System.Text;
using System.Text.RegularExpressions;

namespace TrendsPulse.Platform.Domain.ValueObjects;

/// <summary>
/// Immutable value object that encapsulates slug generation and validation rules.
/// Enforces the invariant that a slug is always lowercase, URL-safe, and non-empty.
/// </summary>
public sealed class Slug : IEquatable<Slug>
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    /// <summary>Creates a Slug from a raw string, applying full sanitisation.</summary>
    public static Slug Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Slug input cannot be empty.", nameof(input));

        var slug = input.Trim().ToLowerInvariant();
        slug = RemoveDiacritics(slug);
        slug = Regex.Replace(slug, @"[\s_]+", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", string.Empty);
        slug = Regex.Replace(slug, @"-{2,}", "-");
        slug = slug.Trim('-');

        if (slug.Length > 200)
            slug = slug[..200].TrimEnd('-');

        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException($"Cannot generate a valid slug from: '{input}'", nameof(input));

        return new Slug(slug);
    }

    /// <summary>Creates a suffix variant: gold-spot → gold-spot-2</summary>
    public Slug WithSuffix(int suffix) => new($"{Value}-{suffix}");

    private static string RemoveDiacritics(string text)
    {
        var normalised = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalised.Length);
        foreach (var c in normalised)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public override string ToString() => Value;
    public bool Equals(Slug? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Slug s && Equals(s);
    public override int GetHashCode() => Value.GetHashCode();
    public static implicit operator string(Slug slug) => slug.Value;
}
