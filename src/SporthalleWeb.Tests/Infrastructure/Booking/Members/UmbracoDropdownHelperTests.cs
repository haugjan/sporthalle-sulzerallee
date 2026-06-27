using SporthalleWeb.Features.Booking;
using SporthalleWeb.Infrastructure.Shared;
using Xunit;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;

namespace SporthalleWeb.Tests.Infrastructure.Booking.Members;

public sealed class UmbracoDropdownHelperTests
{
    // ── null / missing value ───────────────────────────────────────────────────

    [Fact]
    public void ParseDropdownValue_Null_ReturnsFallback()
    {
        Assert.Equal("Privatperson", UmbracoDropdownHelper.ParseDropdownValue(null, "Privatperson"));
    }

    // ── plain string (pre-Umbraco-16 or manually set values) ──────────────────

    [Theory]
    [InlineData("Privatperson")]
    [InlineData("Verein")]
    [InlineData("Firma")]
    [InlineData("Schule")]
    public void ParseDropdownValue_PlainString_ReturnsAsIs(string raw)
    {
        Assert.Equal(raw, UmbracoDropdownHelper.ParseDropdownValue(raw, "fallback"));
    }

    // ── JSON array format (Umbraco FlexDropdown persistent format) ────────────

    [Theory]
    [InlineData("[\"Privatperson\"]", "Privatperson")]
    [InlineData("[\"Verein\"]", "Verein")]
    [InlineData("[\"Firma\"]", "Firma")]
    public void ParseDropdownValue_SingleElementJsonArray_ReturnsFirstElement(string raw, string expected)
    {
        Assert.Equal(expected, UmbracoDropdownHelper.ParseDropdownValue(raw, "fallback"));
    }

    [Fact]
    public void ParseDropdownValue_MultipleElementJsonArray_ReturnsFirstElement()
    {
        // FlexDropdown is configured with multiple: false, so this should not happen in practice,
        // but the parser is defensive and returns the first element regardless.
        Assert.Equal("Verein", UmbracoDropdownHelper.ParseDropdownValue("[\"Verein\",\"Firma\"]", "fallback"));
    }

    // ── empty / degenerate JSON arrays ────────────────────────────────────────

    [Fact]
    public void ParseDropdownValue_EmptyJsonArray_ReturnsFallback()
    {
        Assert.Equal("Privatperson", UmbracoDropdownHelper.ParseDropdownValue("[]", "Privatperson"));
    }

    [Fact]
    public void ParseDropdownValue_InvalidJsonStartingWithBracket_ReturnsFallback()
    {
        Assert.Equal("Privatperson", UmbracoDropdownHelper.ParseDropdownValue("[invalid", "Privatperson"));
    }

    // ── regression: the original bug ──────────────────────────────────────────

    [Fact]
    public void ParseDropdownValue_UmbracoFlexDropdownFormat_ProducesValidRenterType()
    {
        // Before the fix, UmbracoHallMembers passed the raw JSON string directly to
        // new RenterType(...), causing DomainException("Unbekannter Mietertyp: [\"Privatperson\"]").
        // After the fix the adapter calls ParseDropdownValue first.
        var raw = "[\"Privatperson\"]";

        var parsed = UmbracoDropdownHelper.ParseDropdownValue(raw, "Privatperson");
        var renterType = new RenterType(parsed);

        Assert.Equal(RenterTypeValue.Privatperson, renterType.Value);
    }

    [Theory]
    [InlineData("[\"Privatperson\"]")]
    [InlineData("[\"Verein\"]")]
    [InlineData("[\"Firma\"]")]
    [InlineData("[\"Schule\"]")]
    public void ParseDropdownValue_AnyValidRenterTypeAsJsonArray_CanBeConstructed(string raw)
    {
        var parsed = UmbracoDropdownHelper.ParseDropdownValue(raw, "Privatperson");

        // Must not throw — all parsed values are valid RenterType labels.
        var renterType = new RenterType(parsed);
        Assert.NotNull(renterType);
    }
}
