using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;
using SporthalleWeb.Infrastructure.Shared;
using Xunit;

namespace SporthalleWeb.Tests.Infrastructure.PassiveMembership;

/// <summary>
/// Regression tests for the bug where Umbraco.DropDown.Flexible stores values as a JSON array
/// (e.g. ["Pending"]) but comparisons in PassiveMemberRepository used plain string equality,
/// causing filtering and domain model reconstruction to silently fail.
/// </summary>
public sealed class PassiveMemberDropdownParsingTests
{
    // ── MemberStatus filtering ─────────────────────────────────────────────────

    [Theory]
    [InlineData("Pending")]
    [InlineData("[\"Pending\"]")]
    public void ParseDropdownValue_PendingStatus_ReturnsPendingString(string raw)
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(raw, MemberStatus.Pending);
        Assert.Equal(MemberStatus.Pending, result);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("[\"Confirmed\"]")]
    public void ParseDropdownValue_ConfirmedStatus_ReturnsConfirmedString(string raw)
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(raw, MemberStatus.Pending);
        Assert.Equal(MemberStatus.Confirmed, result);
    }

    [Theory]
    [InlineData("Deleted")]
    [InlineData("[\"Deleted\"]")]
    public void ParseDropdownValue_DeletedStatus_ReturnsDeletedString(string raw)
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(raw, MemberStatus.Pending);
        Assert.Equal(MemberStatus.Deleted, result);
    }

    [Fact]
    public void ParseDropdownValue_NullStatus_ReturnsFallback()
    {
        var result = UmbracoDropdownHelper.ParseDropdownValue(null, MemberStatus.Pending);
        Assert.Equal(MemberStatus.Pending, result);
    }

    // ── MembershipLevel parsing ────────────────────────────────────────────────

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

    // ── Regression: before the fix, JSON array strings reached MembershipLevel.FromKey ──

    [Theory]
    [InlineData("[\"Bronze\"]")]
    [InlineData("[\"Silber\"]")]
    [InlineData("[\"Gold\"]")]
    public void MembershipLevelFromKey_WithJsonArrayString_ThrowsDomainException(string jsonArray)
    {
        // This documents the root cause: passing the raw JSON array to FromKey throws.
        // The fix is to call ParseDropdownValue first.
        Assert.Throws<DomainException>(() => MembershipLevel.FromKey(jsonArray));
    }

    [Theory]
    [InlineData("[\"Pending\"]")]
    [InlineData("[\"Confirmed\"]")]
    [InlineData("[\"Deleted\"]")]
    public void StatusComparison_RawJsonArrayString_DoesNotEqualPlainString(string jsonArray)
    {
        // Documents why the old filtering code (GetValue<string>("status") == "Pending")
        // silently returned zero results: the JSON array string never equals the plain key.
        Assert.NotEqual(MemberStatus.Pending, jsonArray);
        Assert.NotEqual(MemberStatus.Confirmed, jsonArray);
        Assert.NotEqual(MemberStatus.Deleted, jsonArray);
    }

    // ── Reconstitute mapping integration ──────────────────────────────────────

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
            1, 42, "Max", "Muster", "Str 1", null, "8400", "Winterthur", "Schweiz",
            null, "max@muster.ch", levelKey, false, null,
            DateTime.UtcNow, MemberStatus.Pending, null, null, null, null, null, null, null);

        Assert.Equal(expectedKey, member.Level.Key);
    }

    [Theory]
    [InlineData("Pending",          MemberStatus.Pending)]
    [InlineData("[\"Pending\"]",    MemberStatus.Pending)]
    [InlineData("Confirmed",        MemberStatus.Confirmed)]
    [InlineData("[\"Confirmed\"]",  MemberStatus.Confirmed)]
    [InlineData("Deleted",          MemberStatus.Deleted)]
    [InlineData("[\"Deleted\"]",    MemberStatus.Deleted)]
    public void Reconstitute_Status_HandlesJsonArrayAndPlainString(string rawStatus, string expectedStatus)
    {
        var status = UmbracoDropdownHelper.ParseDropdownValue(rawStatus, MemberStatus.Pending) ?? MemberStatus.Pending;
        var member = PassiveMember.Reconstitute(
            1, 42, "Max", "Muster", "Str 1", null, "8400", "Winterthur", "Schweiz",
            null, "max@muster.ch", "Bronze", false, null,
            DateTime.UtcNow, status, null, null, null, null, null, null, null);

        Assert.Equal(expectedStatus, member.Status);
    }
}
