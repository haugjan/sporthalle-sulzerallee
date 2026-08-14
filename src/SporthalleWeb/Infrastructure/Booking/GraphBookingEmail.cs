using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.Booking;

public sealed class GraphBookingEmail(GraphMailClient graph) : IBookingEmail
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    private const string SenderEmail = "reservation@sporthalle-sulzerallee.ch";
    private const string SenderName = "Sporthalle Sulzerallee";
    private const string BccEmail = "reservation@sporthalle-sulzerallee.ch";

    public Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Deine Reservationsanfrage für <strong>{FormatSlot(slot)}</strong> ist bei uns eingegangen und wird geprüft.";
        return SendAsync(member.Email.Value, contactName,
            "Reservationsanfrage erhalten – Sporthalle Sulzerallee",
            EmailLayout.Render(
                title: "Reservationsanfrage erhalten",
                greeting: customEmailBody is null ? $"Hallo {contactName}" : null,
                body: body,
                details: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Du erhältst eine separate Bestätigung, sobald die Reservation genehmigt wurde." : null));
    }

    public Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Deine Reservation für <strong>{FormatSlot(slot)}</strong> wurde bestätigt.";
        return SendAsync(member.Email.Value, contactName,
            "Reservation bestätigt – Sporthalle Sulzerallee",
            EmailLayout.Render(
                title: "Reservation bestätigt",
                greeting: customEmailBody is null ? $"Hallo {contactName}" : null,
                body: body,
                details: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Bei Fragen wende dich bitte an reservation@sporthalle-sulzerallee.ch." : null));
    }

    public Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Leider können wir deine Reservationsanfrage für <strong>{FormatSlot(slot)}</strong> nicht bestätigen.";
        return SendAsync(member.Email.Value, contactName,
            "Reservationsanfrage abgelehnt – Sporthalle Sulzerallee",
            EmailLayout.Render(
                title: "Reservationsanfrage abgelehnt",
                greeting: customEmailBody is null ? $"Hallo {contactName}" : null,
                body: body,
                note: customEmailBody is null ? "Bitte kontaktiere uns unter reservation@sporthalle-sulzerallee.ch für weitere Informationen oder einen alternativen Termin." : null));
    }

    private Task SendAsync(string toEmail, string toName, string subject, string htmlContent) =>
        graph.SendAsync(SenderEmail, SenderName, toEmail, toName, subject, htmlContent, BccEmail);

    private static string ContactName(HallMember member) =>
        $"{member.ContactFirstName} {member.ContactLastName}".Trim();

    private string FormatSlot(BookingSlot slot)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, Zurich);
        var end = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc, Zurich);
        return $"{start:dddd, d. MMMM yyyy, HH:mm} – {end:HH:mm} Uhr";
    }
}
