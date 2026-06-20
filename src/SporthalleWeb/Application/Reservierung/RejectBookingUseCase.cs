using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class RejectBookingUseCase(
    IBookingSlotRepository slotRepo,
    IBookingAuditRepository audit,
    IMemberManagerPort members,
    IBookingEmailPort email)
{
    public async Task ExecuteAsync(int slotId, string reason, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var oldStatus = slot.Status.ToString();
        slot.Reject();
        await slotRepo.UpdateAsync(slot);

        if (slot.MemberId.HasValue)
        {
            var member = await members.FindByIdAsync(slot.MemberId.Value);
            if (member is not null)
                await email.SendBookingRejectedToRenterAsync(slot, member);
        }

        await audit.LogAsync("BookingSlot", slotId, "Rejected", adminUser,
            new { Status = oldStatus, Reason = reason }, new { Status = slot.Status.ToString() });
    }
}
