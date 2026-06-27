using SporthalleWeb.Features.PassiveMembership.Registration;
using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

public sealed class PassiveMemberAdmin(
    IPassiveMembers repo,
    IPassiveMemberExport excel,
    IPassiveMemberAbaninja abaninja)
{
    public async Task<IReadOnlyList<PassiveMember>> GetPendingAsync()
        => await repo.GetPendingAsync();

    public async Task<IReadOnlyList<PassiveMember>> GetConfirmedAsync()
        => await repo.GetConfirmedAsync();

    public async Task ConfirmAsync(int memberId, bool isPaid, string confirmedBy)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.Confirm(confirmedBy, isPaid, confirmedBy);
        await repo.UpdateAsync(member);
    }

    public async Task SoftDeleteAsync(int memberId)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.SoftDelete();
        await repo.UpdateAsync(member);
    }

    public async Task MarkAsPaidAsync(int memberId, string paidBy)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.MarkAsPaid(paidBy);
        await repo.UpdateAsync(member);
    }

    public async Task MarkAsUnpaidAsync(int memberId)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.MarkAsUnpaid();
        await repo.UpdateAsync(member);
    }

    public async Task MarkAsExportedToAccountingAsync(int memberId, string by)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.MarkAsExportedToAccounting(by);
        await repo.UpdateAsync(member);
    }

    public async Task UnmarkAsExportedToAccountingAsync(int memberId)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.UnmarkAsExportedToAccounting();
        await repo.UpdateAsync(member);
    }

    public async Task UpdateNotesAsync(int memberId, string? notes)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.UpdateNotes(notes);
        await repo.UpdateAsync(member);
    }

    public async Task<byte[]> ExportExcelAsync()
        => excel.ExportMembers(await repo.GetConfirmedAsync());

    public async Task<byte[]> ExportAbaninjaAsync()
        => abaninja.ExportMembers(await repo.GetConfirmedAsync());
}
