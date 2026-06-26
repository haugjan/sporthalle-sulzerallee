using SporthalleWeb.Domain.Booking;
using Xunit;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class RenterTypeTests
{
    [Theory]
    [InlineData("Verein", RenterTypeValue.Verein)]
    [InlineData("Firma", RenterTypeValue.Firma)]
    [InlineData("Privatperson", RenterTypeValue.Privatperson)]
    [InlineData("Schule", RenterTypeValue.Schule)]
    [InlineData("Behörde", RenterTypeValue.Behörde)]
    public void Constructor_ValidString_ParsesCorrectly(string raw, RenterTypeValue expected)
    {
        var renterType = new RenterType(raw);
        Assert.Equal(expected, renterType.Value);
    }

    [Theory]
    [InlineData("verein")]
    [InlineData("")]
    [InlineData("GmbH")]
    public void Constructor_InvalidString_ThrowsDomainException(string raw)
    {
        Assert.Throws<DomainException>(() => new RenterType(raw));
    }
}
