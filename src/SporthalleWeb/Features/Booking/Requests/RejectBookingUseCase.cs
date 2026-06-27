using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class RejectBooking(
    IBookingSlots slotRepo,
    IBookingAudit audit,
    IHallMembers members,
    IBookingEmail email)
{
    public async Task ExecuteAsync(int slotId, string reason, string adminUser, string? customEmailBody = null, bool sendEmail = true)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        if (slot.Type != SlotType.Reserved)
            throw new DomainException("Nur reservierte Buchungen können abgelehnt werden.");

        if (sendEmail && slot.MemberId.HasValue)
        {
            var member = await members.FindByIdAsync(slot.MemberId.Value);
            if (member is not null)
                await email.SendBookingRejectedToRenterAsync(slot, member, customEmailBody);
        }

        slot.Reject();
        await slotRepo.UpdateAsync(slot);
        await audit.LogAsync("BookingSlot", slotId, "Rejected", adminUser,
            new { Type = "Rejected", Reason = reason }, null);
    }
}
