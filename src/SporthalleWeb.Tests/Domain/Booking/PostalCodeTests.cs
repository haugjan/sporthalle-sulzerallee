using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class PostalCodeTests
{
    [Theory]
    [InlineData("8400", null)]
    [InlineData("1000", "Schweiz")]
    [InlineData("9999", "CH")]
    [InlineData(" 8000 ", "Switzerland")]
    public void Create_ValidSwiss_Succeeds(string input, string? country)
    {
        var pc = PostalCode.Create(input, country);
        Assert.Equal(input.Trim(), pc.Value);
    }

    [Theory]
    [InlineData("123")]      // too short
    [InlineData("12345")]    // too long
    [InlineData("0999")]     // below 1000
    [InlineData("84a0")]     // non-digit
    [InlineData("")]         // empty
    public void Create_InvalidSwiss_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => PostalCode.Create(input, "Schweiz"));
    }

    [Theory]
    [InlineData("12345", "Deutschland")]
    [InlineData("SW1A 1AA", "United Kingdom")]
    public void Create_ForeignLenient_Succeeds(string input, string country)
    {
        var pc = PostalCode.Create(input, country);
        Assert.Equal(input.Trim(), pc.Value);
    }

    [Fact]
    public void Create_ForeignTooLong_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => PostalCode.Create("12345678901", "Deutschland"));
    }

    [Theory]
    [InlineData("not-a-plz")]
    [InlineData("")]
    public void FromPersistence_NeverThrows(string stored)
    {
        var pc = PostalCode.FromPersistence(stored);
        Assert.Equal(stored.Trim(), pc.Value);
    }

    [Fact]
    public void Records_WithSameValue_AreEqual()
    {
        Assert.Equal(PostalCode.Create("8400"), PostalCode.Create("8400"));
    }
}
