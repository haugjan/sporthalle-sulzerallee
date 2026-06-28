using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
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
        Assert.NotEqual(MemberStatus.Pending.Key, jsonArray);
        Assert.NotEqual(MemberStatus.Confirmed.Key, jsonArray);
        Assert.NotEqual(MemberStatus.Deleted.Key, jsonArray);
    }

    // ── Crash scenarios that require defensive reconstitution ─────────────────

    [Fact]
    public void Reconstitute_NullLevelKey_ThrowsDomainException()
    {
        // UmbracoDropdownHelper.ParseDropdownValue(null, null) returns null when
        // a member's membershipLevel property is unset. Without the try/catch in
        // UmbracoPassiveMembers.ReconstituteOrNull, this unhandled exception propagates
        // through OnInitializedAsync and causes the admin page to return HTTP 500.
        var nullLevel = UmbracoDropdownHelper.ParseDropdownValue(null, null);
        Assert.Throws<DomainException>(() => PassiveMember.Reconstitute(
            1, 42, "Max", "Muster", "Str 1", null, "8400", "Winterthur", "Schweiz",
            null, "max@muster.ch", nullLevel!, false, null,
            DateTime.UtcNow, "Pending", null, null, null, null, null, null, null));
    }

    [Fact]
    public void Reconstitute_InvalidFieldNumber_ThrowsDomainException()
    {
        // When int.TryParse fails for the fieldNumber property, the default value 0
        // is used. Without the try/catch, FieldNumber's range check throws, crashing
        // the admin page for every request that enumerates members.
        Assert.Throws<DomainException>(() => PassiveMember.Reconstitute(
            1, 0, "Max", "Muster", "Str 1", null, "8400", "Winterthur", "Schweiz",
            null, "max@muster.ch", "Bronze", false, null,
            DateTime.UtcNow, "Pending", null, null, null, null, null, null, null));
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
            1, 42, "Max", "Muster", "Str 1", null, "8400", "Winterthur", "Schweiz",
            null, "max@muster.ch", "Bronze", false, null,
            DateTime.UtcNow, status, null, null, null, null, null, null, null);

        Assert.Equal(expectedStatusKey, member.Status.Key);
    }
}
