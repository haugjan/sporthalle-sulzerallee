using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class MembershipLevelTests
{
    [Theory]
    [InlineData("Bronze", "Hallenbodenbesitzer", 50)]
    [InlineData("Silber", "Chnebler", 100)]
    [InlineData("Gold", "Cüpli-Chnebler", 200)]
    public void FromKey_ValidKey_ReturnsCorrectLevel(string key, string displayName, decimal fee)
    {
        var level = MembershipLevel.FromKey(key);
        Assert.Equal(key, level.Key);
        Assert.Equal(displayName, level.DisplayName);
        Assert.Equal(fee, level.YearlyFee);
    }

    [Theory]
    [InlineData("bronze")]
    [InlineData("GOLD")]
    [InlineData("Platin")]
    [InlineData("")]
    public void FromKey_UnknownKey_ThrowsDomainException(string key)
    {
        Assert.Throws<DomainException>(() => MembershipLevel.FromKey(key));
    }

    [Fact]
    public void StaticInstances_ReturnSameReference()
    {
        Assert.Same(MembershipLevel.Bronze, MembershipLevel.FromKey("Bronze"));
        Assert.Same(MembershipLevel.Silber, MembershipLevel.FromKey("Silber"));
        Assert.Same(MembershipLevel.Gold, MembershipLevel.FromKey("Gold"));
    }
}
