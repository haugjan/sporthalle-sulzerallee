using System.Globalization;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class CreateBooking(
    IBookingSlots slotRepo,
    IHallMembers members,
    IBookingAudit audit,
    IBookingEmail email,
    HallConfigService hallConfig)
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
            cmd.MemberId, slot, cmd.Title, cmd.Color ?? "#0078D4", cmd.Notes, member.Email);

        booking = await slotRepo.CheckConflictAndSaveAsync(booking, slot);

        await audit.LogAsync("BookingSlot", booking.Id, "Created",
            member.Email, null,
            new { Type = booking.Type.ToString(), slot.StartUtc, slot.EndUtc });

        var reservationText = await hallConfig.GetAsync("mail_reservation_text");
        string? customBody = null;
        if (!string.IsNullOrWhiteSpace(reservationText))
        {
            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(booking.Slot.StartUtc, Zurich);
            var endLocal = TimeZoneInfo.ConvertTimeFromUtc(booking.Slot.EndUtc, Zurich);
            customBody = reservationText
                .Replace("{Name}", $"{member.ContactFirstName} {member.ContactLastName}".Trim())
                .Replace("{Anlass}", booking.Title)
                .Replace("{Datum}", startLocal.ToString("dddd, d. MMMM yyyy", DeCh))
                .Replace("{Von}", startLocal.ToString("HH:mm"))
                .Replace("{Bis}", endLocal.ToString("HH:mm"));
        }

        await email.SendProvisionConfirmationToRenterAsync(booking, member, customBody);
        await email.SendAdminNewBookingNotificationAsync(booking, member);

        return booking;
    }
}
