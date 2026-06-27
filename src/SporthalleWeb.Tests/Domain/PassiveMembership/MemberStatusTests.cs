using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class MemberStatusTests
{
    [Theory]
    [InlineData("Pending", MemberStatusValue.Pending)]
    [InlineData("Confirmed", MemberStatusValue.Confirmed)]
    [InlineData("Deleted", MemberStatusValue.Deleted)]
    [InlineData("pending", MemberStatusValue.Pending)]   // case-insensitive
    public void FromKey_ValidKey_ReturnsStatus(string key, MemberStatusValue expected)
    {
        Assert.Equal(expected, MemberStatus.FromKey(key).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Unknown")]
    public void FromKey_InvalidKey_ThrowsDomainException(string? key)
    {
        Assert.Throws<DomainException>(() => MemberStatus.FromKey(key));
    }

    [Fact]
    public void StaticInstances_HaveExpectedKeys()
    {
        Assert.Equal("Pending", MemberStatus.Pending.Key);
        Assert.Equal("Confirmed", MemberStatus.Confirmed.Key);
        Assert.Equal("Deleted", MemberStatus.Deleted.Key);
    }

    [Fact]
    public void Records_WithSameValue_AreEqual()
    {
        Assert.Equal(MemberStatus.Confirmed, MemberStatus.FromKey("Confirmed"));
    }
}
