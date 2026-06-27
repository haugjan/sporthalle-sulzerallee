using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class ConfirmBooking(
    IBookingSlots slotRepo,
    IBookingAudit audit,
    IHallMembers members,
    IBookingEmail email)
{
    public async Task ExecuteAsync(int slotId, string adminUser, string? customEmailBody = null, bool sendEmail = true)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var oldType = slot.Type.ToString();
        slot.Confirm();
        await slotRepo.UpdateAsync(slot);

        if (sendEmail && slot.MemberId.HasValue)
        {
            var member = await members.FindByIdAsync(slot.MemberId.Value);
            if (member is not null)
                await email.SendBookingConfirmedToRenterAsync(slot, member, customEmailBody);
        }

        await audit.LogAsync("BookingSlot", slotId, "Confirmed", adminUser,
            new { Type = oldType }, new { Type = slot.Type.ToString() });
    }
}
