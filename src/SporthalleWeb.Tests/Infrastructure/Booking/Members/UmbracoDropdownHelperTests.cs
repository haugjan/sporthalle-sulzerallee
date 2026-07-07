using SporthalleWeb.Features.Booking;
using SporthalleWeb.Infrastructure.Shared;
using Xunit;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;

namespace SporthalleWeb.Tests.Infrastructure.Booking.Members;

public sealed class UmbracoDropdownHelperTests
{
    [Fact]
    public void ParseDropdownValue_Null_ReturnsFallback()
    {
        Assert.Equal("Privatperson", UmbracoDropdownHelper.ParseDropdownValue(null, "Privatperson"));
    }

    [Theory]
    [InlineData("Privatperson")]
    [InlineData("Verein")]
    [InlineData("Firma")]
    [InlineData("Schule")]
    public void ParseDropdownValue_PlainString_ReturnsAsIs(string raw)
    {
        Assert.Equal(raw, UmbracoDropdownHelper.ParseDropdownValue(raw, "fallback"));
    }

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
        Assert.Equal("Verein", UmbracoDropdownHelper.ParseDropdownValue("[\"Verein\",\"Firma\"]", "fallback"));
    }

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

    [Fact]
    public void ParseDropdownValue_UmbracoFlexDropdownFormat_ProducesValidRenterType()
    {
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

        var renterType = new RenterType(parsed);
        Assert.NotNull(renterType);
    }
}
