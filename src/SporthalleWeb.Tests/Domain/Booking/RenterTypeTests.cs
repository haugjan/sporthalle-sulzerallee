using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class RenterTypeTests
{
    [Theory]
    [InlineData("Verein", RenterTypeValue.Verein)]
    [InlineData("Firma", RenterTypeValue.Firma)]
    [InlineData("Privatperson", RenterTypeValue.Privatperson)]
    [InlineData("Schule", RenterTypeValue.Schule)]
    public void Constructor_ValidString_ParsesCorrectly(string raw, RenterTypeValue expected)
    {
        var renterType = new RenterType(raw);
        Assert.Equal(expected, renterType.Value);
    }

    [Theory]
    [InlineData("verein")]
    [InlineData("")]
    [InlineData("GmbH")]
    [InlineData("Behörde")]
    public void Constructor_InvalidString_ThrowsDomainException(string raw)
    {
        Assert.Throws<DomainException>(() => new RenterType(raw));
    }

    [Fact]
    public void Constructor_UmbracoJsonArrayFormat_ThrowsDomainException()
    {
        // Umbraco FlexDropdown stores values as JSON arrays: ["Privatperson"].
        // RenterType does not parse JSON — UmbracoDropdownHelper must strip the array
        // format before the value reaches this constructor.
        Assert.Throws<DomainException>(() => new RenterType("[\"Privatperson\"]"));
    }
}
