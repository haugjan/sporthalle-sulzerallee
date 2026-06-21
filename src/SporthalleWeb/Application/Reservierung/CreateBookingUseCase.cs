using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class CreateBookingUseCase(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members,
    IBookingAuditRepository audit,
    IBookingEmailPort email)
{
    public async Task<BookingSlot> ExecuteAsync(CreateBookingCommand cmd)
    {
        var member = await members.FindByIdAsync(cmd.MemberId)
            ?? throw new DomainException("Mieter nicht gefunden.");

        var slot = new TimeSlot(cmd.StartUtc, cmd.EndUtc);

        var booking = BookingSlot.CreateReserved(
            cmd.MemberId, slot, cmd.Title, cmd.Color ?? "#0078D4", cmd.Notes, member.Email);

        booking = await slotRepo.CheckConflictAndSaveAsync(booking, slot);

        await audit.LogAsync("BookingSlot", booking.Id, "Created",
            member.Email, null,
            new { Type = booking.Type.ToString(), slot.StartUtc, slot.EndUtc });

        await email.SendProvisionConfirmationToRenterAsync(booking, member);
        await email.SendAdminNewBookingNotificationAsync(booking, member);

        return booking;
    }
}
