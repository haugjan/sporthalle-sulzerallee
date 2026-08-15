using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;
using SporthalleWeb.Features.Email;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.Booking;

public sealed class BookingEmailSender(IEmailOutbox outbox, IHallConfigStore config) : IBookingEmail
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    private const string SenderEmail = "reservation@sporthalle-sulzerallee.ch";
    private const string SenderName = "Reservation Sporthalle Sulzerallee";
    private const string BccEmail = "reservation@sporthalle-sulzerallee.ch";

    public async Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var subject = await config.GetAsync("mail_reservation_betreff") is { Length: > 0 } s
            ? s : "Reservationsanfrage erhalten – Sporthalle Sulzerallee";
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Deine Reservationsanfrage für <strong>{FormatSlot(slot)}</strong> ist bei uns eingegangen und wird geprüft.";
        await EnqueueAsync(member.Email.Value, contactName,
            subject,
            EmailLayout.Render(
                title: "Reservationsanfrage erhalten",
                greeting: customEmailBody is null ? $"Hallo {contactName}" : null,
                body: body,
                details: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Du erhältst eine separate Bestätigung, sobald die Reservation genehmigt wurde." : null),
            slot.Id.ToString());
    }

    public async Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var subject = await config.GetAsync("mail_bestaetigung_betreff") is { Length: > 0 } s
            ? s : "Reservation bestätigt – Sporthalle Sulzerallee";
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Deine Reservation für <strong>{FormatSlot(slot)}</strong> wurde bestätigt.";
        await EnqueueAsync(member.Email.Value, contactName,
            subject,
            EmailLayout.Render(
                title: "Reservation bestätigt",
                greeting: customEmailBody is null ? $"Hallo {contactName}" : null,
                body: body,
                details: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Bei Fragen wende dich bitte an reservation@sporthalle-sulzerallee.ch." : null),
            slot.Id.ToString());
    }

    public async Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Leider können wir deine Reservationsanfrage für <strong>{FormatSlot(slot)}</strong> nicht bestätigen.";
        await EnqueueAsync(member.Email.Value, contactName,
            "Reservationsanfrage abgelehnt – Sporthalle Sulzerallee",
            EmailLayout.Render(
                title: "Reservationsanfrage abgelehnt",
                greeting: customEmailBody is null ? $"Hallo {contactName}" : null,
                body: body,
                note: customEmailBody is null ? "Bitte kontaktiere uns unter reservation@sporthalle-sulzerallee.ch für weitere Informationen oder einen alternativen Termin." : null),
            slot.Id.ToString());
    }

    private Task EnqueueAsync(string toEmail, string toName, string subject, string htmlBody, string? reference = null) =>
        outbox.EnqueueAsync(new OutboxEnqueueRequest(
            SenderEmail, SenderName, toEmail, toName, BccEmail, subject, htmlBody,
            Source: "Buchung", Reference: reference));

    private static string ContactName(HallMember member) =>
        $"{member.ContactFirstName} {member.ContactLastName}".Trim();

    private string FormatSlot(BookingSlot slot)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, Zurich);
        var end = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc, Zurich);
        return $"{start:dddd, d. MMMM yyyy, HH:mm} – {end:HH:mm} Uhr";
    }
}
