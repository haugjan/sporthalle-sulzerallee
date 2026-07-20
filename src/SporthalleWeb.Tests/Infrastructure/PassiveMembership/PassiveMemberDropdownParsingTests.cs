using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using SporthalleWeb.Infrastructure.Shared;
using Xunit;

namespace SporthalleWeb.Tests.Infrastructure.PassiveMembership;

public sealed class PassiveMemberDropdownParsingTests
{
    [Theory]
    [InlineData("Pending")]
    [InlineData("[\"Pending\"]")]
    public void ParseDropdownValue_PendingStatus_ReturnsPendingString(string raw)
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(raw, MemberStatus.Pending.Key);
        Assert.Equal(MemberStatus.Pending.Key, result);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("[\"Confirmed\"]")]
    public void ParseDropdownValue_ConfirmedStatus_ReturnsConfirmedString(string raw)
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(raw, MemberStatus.Pending.Key);
        Assert.Equal(MemberStatus.Confirmed.Key, result);
    }

    [Theory]
    [InlineData("Deleted")]
    [InlineData("[\"Deleted\"]")]
    public void ParseDropdownValue_DeletedStatus_ReturnsDeletedString(string raw)
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(raw, MemberStatus.Pending.Key);
        Assert.Equal(MemberStatus.Deleted.Key, result);
    }

    [Fact]
    public void ParseDropdownValue_NullStatus_ReturnsFallback()
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(null, MemberStatus.Pending.Key);
        Assert.Equal(MemberStatus.Pending.Key, result);
    }

    [Theory]
    [InlineData("Bronze")]
    [InlineData("[\"Bronze\"]")]
    public void ParseDropdownValue_ThenFromKey_BronzeJsonArray_Succeeds(string raw)
    {
        var parsed = UmbracoDropdownHelper.ParseDropdownValue(raw, "Bronze");
        var level = MembershipLevel.FromKey(parsed);
        Assert.Equal(MembershipLevel.Bronze, level);
    }

    [Theory]
    [InlineData("Silber")]
    [InlineData("[\"Silber\"]")]
    public void ParseDropdownValue_ThenFromKey_SilberJsonArray_Succeeds(string raw)
    {
        var parsed = UmbracoDropdownHelper.ParseDropdownValue(raw, "Bronze");
        var level = MembershipLevel.FromKey(parsed);
        Assert.Equal(MembershipLevel.Silber, level);
    }

    [Theory]
    [InlineData("Gold")]
    [InlineData("[\"Gold\"]")]
    public void ParseDropdownValue_ThenFromKey_GoldJsonArray_Succeeds(string raw)
    {
        var parsed = UmbracoDropdownHelper.ParseDropdownValue(raw, "Bronze");
        var level = MembershipLevel.FromKey(parsed);
        Assert.Equal(MembershipLevel.Gold, level);
    }

    [Theory]
    [InlineData("[\"Bronze\"]")]
    [InlineData("[\"Silber\"]")]
    [InlineData("[\"Gold\"]")]
    public void MembershipLevelFromKey_WithJsonArrayString_ThrowsDomainException(string jsonArray)
    {
        Assert.Throws<DomainException>(() => MembershipLevel.FromKey(jsonArray));
    }

    [Theory]
    [InlineData("[\"Pending\"]")]
    [InlineData("[\"Confirmed\"]")]
    [InlineData("[\"Deleted\"]")]
    public void StatusComparison_RawJsonArrayString_DoesNotEqualPlainString(string jsonArray)
    {
        Assert.NotEqual(MemberStatus.Pending.Key, jsonArray);
        Assert.NotEqual(MemberStatus.Confirmed.Key, jsonArray);
        Assert.NotEqual(MemberStatus.Deleted.Key, jsonArray);
    }

    [Fact]
    public void Reconstitute_NullLevelKey_ThrowsDomainException()
    {
        var nullLevel = UmbracoDropdownHelper.ParseDropdownValue(null, null);
        Assert.Throws<DomainException>(() => PassiveMember.Reconstitute(
            1, 42, "Max", "Muster", null,"max@muster.ch", nullLevel!, false, null,
            DateTime.UtcNow, "Pending", null, null, null, null, null, null, null));
    }

    [Fact]
    public void Reconstitute_InvalidFieldNumber_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => PassiveMember.Reconstitute(
            1, 0, "Max", "Muster", null,"max@muster.ch", "Bronze", false, null,
            DateTime.UtcNow, "Pending", null, null, null, null, null, null, null));
    }

    [Theory]
    [InlineData("Bronze",         "Bronze")]
    [InlineData("[\"Bronze\"]",   "Bronze")]
    [InlineData("Silber",         "Silber")]
    [InlineData("[\"Silber\"]",   "Silber")]
    [InlineData("Gold",           "Gold")]
    [InlineData("[\"Gold\"]",     "Gold")]
    public void Reconstitute_LevelKey_HandlesJsonArrayAndPlainString(string rawLevel, string expectedKey)
    {
        var levelKey = UmbracoDropdownHelper.ParseDropdownValue(rawLevel, "Bronze") ?? "Bronze";
        var member = PassiveMember.Reconstitute(
            1, 42, "Max", "Muster", null,"max@muster.ch", levelKey, false, null,
            DateTime.UtcNow, MemberStatus.Pending.Key, null, null, null, null, null, null, null);

        Assert.Equal(expectedKey, member.Level.Key);
    }

    [Theory]
    [InlineData("Pending",          "Pending")]
    [InlineData("[\"Pending\"]",    "Pending")]
    [InlineData("Confirmed",        "Confirmed")]
    [InlineData("[\"Confirmed\"]",  "Confirmed")]
    [InlineData("Deleted",          "Deleted")]
    [InlineData("[\"Deleted\"]",    "Deleted")]
    public void Reconstitute_Status_HandlesJsonArrayAndPlainString(string rawStatus, string expectedStatusKey)
    {
        var status = UmbracoDropdownHelper.ParseDropdownValue(rawStatus, MemberStatus.Pending.Key) ?? MemberStatus.Pending.Key;
        var member = PassiveMember.Reconstitute(
            1, 42, "Max", "Muster", null,"max@muster.ch", "Bronze", false, null,
            DateTime.UtcNow, status, null, null, null, null, null, null, null);

        Assert.Equal(expectedStatusKey, member.Status.Key);
    }
}
