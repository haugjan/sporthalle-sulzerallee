using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class MemberEmailTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("USER@EXAMPLE.COM", "user@example.com")]
    [InlineData("  user@example.com  ", "user@example.com")]
    public void Constructor_ValidEmail_NormalizesToLowercase(string input, string expected)
    {
        var email = new MemberEmail(input);
        Assert.Equal(expected, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missingatsign.com")]
    public void Constructor_InvalidEmail_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => new MemberEmail(input));
    }

    [Fact]
    public void Records_WithSameNormalizedValue_AreEqual()
    {
        Assert.Equal(new MemberEmail("A@B.COM"), new MemberEmail("a@b.com"));
    }
}
