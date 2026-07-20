using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Xunit;

namespace SporthalleWeb.Tests.Domain.PassiveMembership;

public sealed class PassiveMemberTests
{
    private static PassiveMember CreateMember(
        bool showNameOnFloor = false,
        string? displayName = null) =>
        PassiveMember.Register(
            new FieldNumber(1),
            "Max", "Muster",
            new MemberEmail("max@muster.ch"),
            MembershipLevel.Bronze,
            showNameOnFloor,
            displayName,
            VipField.Default);

    [Fact]
    public void Register_ValidData_CreatesPendingMember()
    {
        var m = CreateMember();
        Assert.Equal(MemberStatus.Pending, m.Status);
        Assert.Equal("Max", m.FirstName);
        Assert.Equal("Muster", m.LastName);
        Assert.Equal(1, m.FieldNumber.Value);
        Assert.Equal("max@muster.ch", m.Email.Value);
        Assert.Equal(MembershipLevel.Bronze, m.Level);
        Assert.Null(m.ConfirmedAt);
        Assert.Null(m.PaidAt);
    }

    [Fact]
    public void Register_VipFieldWithBronze_Throws()
    {
        Assert.Throws<DomainException>(() => PassiveMember.Register(
            new FieldNumber(502), "Max", "Muster",
            new MemberEmail("max@muster.ch"),
            MembershipLevel.Bronze, false, null, VipField.Default));
    }

    [Theory]
    [InlineData("Silber")]
    [InlineData("Gold")]
    public void Register_VipFieldWithSilberOrGold_Succeeds(string levelKey)
    {
        var m = PassiveMember.Register(
            new FieldNumber(502), "Max", "Muster",
            new MemberEmail("max@muster.ch"),
            MembershipLevel.FromKey(levelKey), false, null, VipField.Default);
        Assert.Equal(levelKey, m.Level.Key);
    }

    [Fact]
    public void Register_CustomSpecialMap_BlocksBronzeOnConfiguredField()
    {
        var map = new SpecialFieldMap([new SpecialArea("Sonderzone", 100, 100)]);
        Assert.Throws<DomainException>(() => PassiveMember.Register(
            new FieldNumber(100), "Max", "Muster",
            new MemberEmail("max@muster.ch"),
            MembershipLevel.Bronze, false, null, map));
    }

    [Fact]
    public void Register_EmptySpecialMap_AllowsBronzeOnDefaultVipField()
    {
        var map = new SpecialFieldMap([]);
        var m = PassiveMember.Register(
            new FieldNumber(502), "Max", "Muster",
            new MemberEmail("max@muster.ch"),
            MembershipLevel.Bronze, false, null, map);
        Assert.Equal("Bronze", m.Level.Key);
    }

    [Fact]
    public void Register_ShowNameOnFloor_RequiresDisplayName()
    {
        Assert.Throws<DomainException>(() => CreateMember(showNameOnFloor: true, displayName: null));
        Assert.Throws<DomainException>(() => CreateMember(showNameOnFloor: true, displayName: "  "));
    }

    [Fact]
    public void Register_ShowNameOnFloor_WithDisplayName_SetsIt()
    {
        var m = CreateMember(showNameOnFloor: true, displayName: "  MaxM  ");
        Assert.Equal("MaxM", m.DisplayName);
        Assert.True(m.ShowNameOnFloor);
    }

    [Fact]
    public void Register_NotShowNameOnFloor_DisplayNameIsNull()
    {
        var m = CreateMember(showNameOnFloor: false, displayName: "irrelevant");
        Assert.Null(m.DisplayName);
    }

    [Fact]
    public void Register_TrimsStringFields()
    {
        var m = PassiveMember.Register(
            new FieldNumber(1), "  Max  ", "  Muster  ",
            new MemberEmail("x@y.com"), MembershipLevel.Bronze, false, null, VipField.Default);
        Assert.Equal("Max", m.FirstName);
        Assert.Equal("Muster", m.LastName);
    }

    [Fact]
    public void Confirm_SetStatusAndTimestamp()
    {
        var m = CreateMember();
        m.Confirm("admin", isPaid: false, paidBy: null);
        Assert.Equal(MemberStatus.Confirmed, m.Status);
        Assert.NotNull(m.ConfirmedAt);
        Assert.Equal("admin", m.ConfirmedBy);
        Assert.Null(m.PaidAt);
    }

    [Fact]
    public void Confirm_WithIsPaid_AlsoSetsPaid()
    {
        var m = CreateMember();
        m.Confirm("admin", isPaid: true, paidBy: "admin");
        Assert.NotNull(m.PaidAt);
        Assert.Equal("admin", m.PaidBy);
    }

    [Fact]
    public void SoftDelete_SetsStatusDeleted()
    {
        var m = CreateMember();
        m.SoftDelete();
        Assert.Equal(MemberStatus.Deleted, m.Status);
    }

    [Fact]
    public void MarkAsPaid_SetsPaidAtAndBy()
    {
        var m = CreateMember();
        m.MarkAsPaid("cashier");
        Assert.NotNull(m.PaidAt);
        Assert.Equal("cashier", m.PaidBy);
    }

    [Fact]
    public void MarkAsUnpaid_ClearsPaidAtAndBy()
    {
        var m = CreateMember();
        m.MarkAsPaid("cashier");
        m.MarkAsUnpaid();
        Assert.Null(m.PaidAt);
        Assert.Null(m.PaidBy);
    }

    [Fact]
    public void MarkAsExportedToAccounting_SetsFlag()
    {
        var m = CreateMember();
        m.MarkAsExportedToAccounting("export-user");
        Assert.True(m.ExportedToAccounting);
        Assert.NotNull(m.ExportedToAccountingAt);
        Assert.Equal("export-user", m.ExportedToAccountingBy);
    }

    [Fact]
    public void UnmarkAsExportedToAccounting_ClearsFlag()
    {
        var m = CreateMember();
        m.MarkAsExportedToAccounting("user");
        m.UnmarkAsExportedToAccounting();
        Assert.False(m.ExportedToAccounting);
        Assert.Null(m.ExportedToAccountingAt);
        Assert.Null(m.ExportedToAccountingBy);
    }

    [Fact]
    public void UpdateNotes_SetsAndTrimsNotes()
    {
        var m = CreateMember();
        m.UpdateNotes("  some notes  ");
        Assert.Equal("some notes", m.Notes);
    }

    [Fact]
    public void UpdateNotes_NullInput_SetsNull()
    {
        var m = CreateMember();
        m.UpdateNotes("old");
        m.UpdateNotes(null);
        Assert.Null(m.Notes);
    }
}
