using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class BookingAdminService(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members,
    IBookingAuditRepository audit)
{
    public async Task<IReadOnlyList<(BookingSlot Slot, HallMember? Member)>> GetPendingAsync()
    {
        var slots = await slotRepo.GetReservedSlotsAsync();
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

    public async Task DeleteSlotAsync(int slotId, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        await slotRepo.DeleteAsync(slotId);
        await audit.LogAsync("BookingSlot", slotId, "Deleted", adminUser,
            new { Type = slot.Type.ToString() }, null);
    }
}
