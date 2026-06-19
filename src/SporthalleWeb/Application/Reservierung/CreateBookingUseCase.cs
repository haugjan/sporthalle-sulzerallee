using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class CreateBookingUseCase(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members,
    IBookingAuditRepository audit,
    IBookingEmailPort email,
    IHallConfigurationPort config)
{
    public async Task<BookingSlot> ExecuteAsync(CreateBookingCommand cmd)
    {
        var member = await members.FindByIdAsync(cmd.MemberId)
            ?? throw new DomainException("Mieter nicht gefunden.");

        var slot = new TimeSlot(cmd.StartUtc, cmd.EndUtc);

        var overlaps = await slotRepo.GetActiveOverlapsAsync(slot);
        if (overlaps.Count > 0)
            throw new SlotConflictException(slot, overlaps);

        var pricePerBlock = await config.GetPricePerBlockAsync();
        var booking = BookingSlot.CreateUserBooking(
            cmd.MemberId, slot, pricePerBlock, cmd.EventType, cmd.Notes, member.Email);

        booking = await slotRepo.SaveAsync(booking);

        await audit.LogAsync("BookingSlot", booking.Id, "Created",
            member.Email, null,
            new { Status = booking.Status.ToString(), slot.StartUtc, slot.EndUtc });

        await email.SendProvisionConfirmationToRenterAsync(booking, member);
        await email.SendAdminNewBookingNotificationAsync(booking, member);

        return booking;
    }
}
