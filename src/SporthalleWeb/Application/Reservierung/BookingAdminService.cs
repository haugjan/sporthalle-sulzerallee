using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence;

namespace SporthalleWeb.Application.Reservierung;

public sealed class BookingAdminService(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members,
    IBookingAuditRepository audit,
    SchoolHolidayRepository holidayRepo)
{
    public async Task<IReadOnlyList<(BookingSlot Slot, HallMember? Member)>> GetPendingAsync()
    {
        var slots = await slotRepo.GetPendingAdminApprovalAsync();
        var result = new List<(BookingSlot, HallMember?)>();
        foreach (var slot in slots)
        {
            HallMember? member = null;
            if (slot.MemberId.HasValue)
                member = await members.FindByIdAsync(slot.MemberId.Value);
            result.Add((slot, member));
        }
        return result;
    }

    public async Task<IReadOnlyList<SchoolHoliday>> GetSchoolHolidaysAsync() =>
        await holidayRepo.GetAllAsync();

    public async Task<SchoolHoliday> AddHolidayAsync(string name, DateOnly from, DateOnly until) =>
        await holidayRepo.SaveAsync(name, from, until);

    public async Task DeleteHolidayAsync(int id) =>
        await holidayRepo.DeleteAsync(id);

    public async Task AdjustPriceAsync(int slotId, decimal newPricePerBlock, string? note, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var oldPrice = slot.PricePerBlock;
        slot.AdjustPrice(newPricePerBlock, note);
        await slotRepo.UpdateAsync(slot);
        await audit.LogAsync("BookingSlot", slotId, "PriceChanged", adminUser,
            new { PricePerBlock = oldPrice },
            new { PricePerBlock = newPricePerBlock, Note = note });
    }

    public async Task CancelSlotAsync(int slotId, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var oldStatus = slot.Status.ToString();
        slot.Cancel();
        await slotRepo.UpdateAsync(slot);
        await audit.LogAsync("BookingSlot", slotId, "Cancelled", adminUser,
            new { Status = oldStatus }, new { Status = slot.Status.ToString() });
    }
}
