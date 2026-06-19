using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

namespace SporthalleWeb.Application.PassivMitgliedschaft;

public sealed class AdminService(
    IPassivMitgliederRepository repo,
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

    public async Task<IReadOnlyList<PassivMitglied>> GetAllAsync()
        => await repo.GetAllAsync();

    public async Task<byte[]> ExportExcelAsync()
        => excel.ExportMembers(await repo.GetAllAsync());

    public async Task<byte[]> ExportAbaninjaAsync()
        => abaninja.ExportMembers(await repo.GetAllAsync());
}
