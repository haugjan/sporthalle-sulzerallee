using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class ConfirmBookingUseCase(
    IBookingSlotRepository slotRepo,
    IBookingAuditRepository audit,
    IMemberManagerPort members,
    IBookingEmailPort email)
{
    public async Task ExecuteAsync(int slotId, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var oldType = slot.Type.ToString();
        slot.Confirm();
        await slotRepo.UpdateAsync(slot);

        if (slot.MemberId.HasValue)
        {
            var member = await members.FindByIdAsync(slot.MemberId.Value);
            if (member is not null)
                await email.SendBookingConfirmedToRenterAsync(slot, member);
        }

        await audit.LogAsync("BookingSlot", slotId, "Confirmed", adminUser,
            new { Type = oldType }, new { Type = slot.Type.ToString() });
    }
}
