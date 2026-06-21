using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;

namespace SporthalleWeb.Application.PassiveMembership;

public sealed class AdminService(
    IPassiveMemberRepository repo,
    IExcelPort excel,
    IAbaninjaCsvPort abaninja)
{
    public async Task MarkAsPaidAsync(int memberId)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.MarkAsPaid();
        await repo.UpdateAsync(member);
    }

    public async Task UpdateNotesAsync(int memberId, string? notes)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.UpdateNotes(notes);
        await repo.UpdateAsync(member);
    }

    public async Task<IReadOnlyList<PassiveMember>> GetAllAsync()
        => await repo.GetAllAsync();

    public async Task<byte[]> ExportExcelAsync()
        => excel.ExportMembers(await repo.GetAllAsync());

    public async Task<byte[]> ExportAbaninjaAsync()
        => abaninja.ExportMembers(await repo.GetAllAsync());
}
