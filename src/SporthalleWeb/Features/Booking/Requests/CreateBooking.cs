using System.Globalization;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Configuration;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Requests;

public sealed class CreateBooking(
    IBookingSlots slotRepo,
    IHallMembers members,
    IBookingAudit audit,
    IBookingEmail email,
    IHallConfigStore hallConfig)
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
    private static readonly CultureInfo DeCh = CultureInfo.GetCultureInfo("de-CH");

    public async Task<BookingSlot> ExecuteAsync(CreateBookingCommand cmd)
    {
        var member = await members.FindByIdAsync(cmd.MemberId)
            ?? throw new DomainException("Mieter nicht gefunden.");

        var slot = new TimeSlot(cmd.StartUtc, cmd.EndUtc);

        var booking = BookingSlot.CreateReserved(
            cmd.MemberId, slot, cmd.Title, cmd.Notes, member.Email.Value);

        booking = await slotRepo.CheckConflictAndSaveAsync(booking, slot);

        await audit.LogAsync("BookingSlot", booking.Id, "Created",
            member.Email.Value, null,
            new { Type = booking.Type.ToString(), slot.StartUtc, slot.EndUtc });

        var reservationText = await hallConfig.GetAsync("mail_reservation_text");
        if (string.IsNullOrWhiteSpace(reservationText))
            reservationText = BookingMailTemplates.ReservationDefault;

        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(booking.Slot.StartUtc, Zurich);
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(booking.Slot.EndUtc, Zurich);
        var customBody = BookingMailTemplates.Apply(
            reservationText,
            member.ContactFirstName,
            $"{member.ContactFirstName} {member.ContactLastName}".Trim(),
            booking.Title,
            startLocal.ToString("dddd, d. MMMM yyyy", DeCh),
            startLocal.ToString("HH:mm"),
            endLocal.ToString("HH:mm"));

        await email.SendProvisionConfirmationToRenterAsync(booking, member, customBody);
        await email.SendAdminNewBookingNotificationAsync(booking, member);

        return booking;
    }
}
