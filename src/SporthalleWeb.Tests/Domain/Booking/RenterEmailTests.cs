using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class RenterEmailTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("USER@EXAMPLE.COM", "user@example.com")]
    [InlineData("  user@example.com  ", "user@example.com")]
    public void Constructor_ValidEmail_NormalizesToLowercase(string input, string expected)
    {
        var email = new RenterEmail(input);
        Assert.Equal(expected, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void Constructor_InvalidEmail_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => new RenterEmail(input));
    }

    [Fact]
    public void Records_WithSameNormalizedValue_AreEqual()
    {
        Assert.Equal(new RenterEmail("A@B.COM"), new RenterEmail("a@b.com"));
    }
}
