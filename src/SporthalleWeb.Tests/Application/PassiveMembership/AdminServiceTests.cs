using Moq;
using SporthalleWeb.Features.PassiveMembership.MemberAdmin;
using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;
using SporthalleWeb.Features.PassiveMembership.Registration;
using Xunit;

namespace SporthalleWeb.Tests.Application.PassiveMembership;

public sealed class AdminServiceTests
{
    private readonly Mock<IPassiveMembers> _repo = new();
    private readonly Mock<IPassiveMemberExport> _excel = new();
    private readonly Mock<IPassiveMemberAbaninja> _abaninja = new();
    private readonly PassiveMemberAdmin _sut;

    public AdminServiceTests()
    {
        _sut = new PassiveMemberAdmin(_repo.Object, _excel.Object, _abaninja.Object);
    }

    private static PassiveMember MakeMember(int id = 1, string status = MemberStatus.Pending) =>
        PassiveMember.Reconstitute(
            id, 1, "Max", "Muster", "Str 1", null, "8400", "Winterthur", "Schweiz",
            null, "max@muster.ch", "Bronze", false, null,
            DateTime.UtcNow, status, null, null, null, null, null, null, null);

    [Fact]
    public async Task MarkAsPaidAsync_MemberNotFound_ThrowsMemberNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(99)).ReturnsAsync((PassiveMember?)null);

        await Assert.ThrowsAsync<MemberNotFoundException>(() => _sut.MarkAsPaidAsync(99, "admin"));
    }

    [Fact]
    public async Task MarkAsPaidAsync_ValidId_SetsPaidAndUpdates()
    {
        var member = MakeMember();
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.MarkAsPaidAsync(1, "cashier");

        Assert.NotNull(member.PaidAt);
        Assert.Equal("cashier", member.PaidBy);
        _repo.Verify(r => r.UpdateAsync(member), Times.Once);
    }

    [Fact]
    public async Task MarkAsUnpaidAsync_ValidId_ClearsPaid()
    {
        var member = MakeMember();
        member.MarkAsPaid("admin");
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.MarkAsUnpaidAsync(1);

        Assert.Null(member.PaidAt);
        _repo.Verify(r => r.UpdateAsync(member), Times.Once);
    }

    [Fact]
    public async Task MarkAsExportedToAccountingAsync_ValidId_SetsFlag()
    {
        var member = MakeMember();
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.MarkAsExportedToAccountingAsync(1, "export-user");

        Assert.True(member.ExportedToAccounting);
        _repo.Verify(r => r.UpdateAsync(member), Times.Once);
    }

    [Fact]
    public async Task UnmarkAsExportedToAccountingAsync_ValidId_ClearsFlag()
    {
        var member = MakeMember();
        member.MarkAsExportedToAccounting("user");
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.UnmarkAsExportedToAccountingAsync(1);

        Assert.False(member.ExportedToAccounting);
    }

    [Fact]
    public async Task UpdateNotesAsync_ValidId_SetsNotes()
    {
        var member = MakeMember();
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.UpdateNotesAsync(1, "new notes");

        Assert.Equal("new notes", member.Notes);
    }

    [Fact]
    public async Task SoftDeleteAsync_ValidId_SetsStatusDeleted()
    {
        var member = MakeMember();
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.SoftDeleteAsync(1);

        Assert.Equal(MemberStatus.Deleted, member.Status);
    }

    [Fact]
    public async Task ConfirmAsync_ValidId_SetsStatusConfirmed()
    {
        var member = MakeMember();
        _repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(member);

        await _sut.ConfirmAsync(1, isPaid: false, confirmedBy: "admin");

        Assert.Equal(MemberStatus.Confirmed, member.Status);
    }

    [Fact]
    public async Task ExportExcelAsync_DelegatesToPort()
    {
        var members = new List<PassiveMember> { MakeMember() };
        _repo.Setup(r => r.GetConfirmedAsync()).ReturnsAsync(members);
        _excel.Setup(e => e.ExportMembers(members)).Returns([0x50, 0x4b]);

        var bytes = await _sut.ExportExcelAsync();

        Assert.Equal([0x50, 0x4b], bytes);
    }

    [Fact]
    public async Task ExportAbaninjaAsync_DelegatesToPort()
    {
        var members = new List<PassiveMember> { MakeMember() };
        _repo.Setup(r => r.GetConfirmedAsync()).ReturnsAsync(members);
        _abaninja.Setup(a => a.ExportMembers(members)).Returns([0x61, 0x62]);

        var bytes = await _sut.ExportAbaninjaAsync();

        Assert.Equal([0x61, 0x62], bytes);
    }
}
