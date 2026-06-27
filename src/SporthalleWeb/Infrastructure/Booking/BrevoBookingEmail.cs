using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.Booking;

public sealed class BrevoBookingEmail(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<BrevoBookingEmail> logger) : IBookingEmail
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    private string AdminEmail => config["Brevo:AdminEmail"] ?? "admin@sporthalle-sulzerallee.ch";
    private string SenderEmail => config["Brevo:SenderEmail"] ?? "noreply@sporthalle-sulzerallee.ch";
    private string SenderName => "Sporthalle Sulzerallee";

    public Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Ihre Buchungsanfrage für <strong>{FormatSlot(slot)}</strong> ist bei uns eingegangen und wird geprüft.";
        return SendAsync(member.Email.Value, contactName,
            "Buchungsanfrage erhalten – Sporthalle Sulzerallee",
            BuildEmail(
                title: "Buchungsanfrage erhalten",
                greeting: $"Guten Tag {contactName}",
                body: body,
                detail: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Sie erhalten eine separate Bestätigung, sobald die Buchung genehmigt wurde." : null));
    }

    public Task SendAdminNewBookingNotificationAsync(BookingSlot slot, HallMember member) =>
        SendAsync(AdminEmail, SenderName,
            $"Neue Buchungsanfrage von {ContactName(member)}",
            BuildEmail(
                title: "Neue Buchungsanfrage",
                greeting: "Hallo",
                body: $"Eine neue Buchungsanfrage ist eingegangen.",
                detail: $"Mieter: {ContactName(member)} ({member.Email.Value})<br>Zeitslot: {FormatSlot(slot)}<br>Anlass: {slot.Title}",
                ctaUrl: "https://www.sporthalle-sulzerallee.ch/umbraco",
                ctaLabel: "Zur Verwaltung"));

    public Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Ihre Buchung für <strong>{FormatSlot(slot)}</strong> wurde bestätigt.";
        return SendAsync(member.Email.Value, contactName,
            "Buchung bestätigt – Sporthalle Sulzerallee",
            BuildEmail(
                title: "Buchung bestätigt",
                greeting: $"Guten Tag {contactName}",
                body: body,
                detail: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Bei Fragen wenden Sie sich bitte an reservation@sporthalle-sulzerallee.ch." : null));
    }

    public Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var contactName = ContactName(member);
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Leider können wir Ihre Buchungsanfrage für <strong>{FormatSlot(slot)}</strong> nicht bestätigen.";
        return SendAsync(member.Email.Value, contactName,
            "Buchungsanfrage abgelehnt – Sporthalle Sulzerallee",
            BuildEmail(
                title: "Buchungsanfrage abgelehnt",
                greeting: $"Guten Tag {contactName}",
                body: body,
                note: customEmailBody is null ? "Bitte kontaktieren Sie uns unter reservation@sporthalle-sulzerallee.ch für weitere Informationen oder einen alternativen Termin." : null));
    }

    private static string ContactName(HallMember member) =>
        $"{member.ContactFirstName} {member.ContactLastName}".Trim();

    // Shared, homepage-aligned layout (see Infrastructure/Shared/EmailLayout.cs).
    private static string BuildEmail(
        string title, string greeting, string body,
        string? detail = null, string? note = null,
        string? ctaUrl = null, string? ctaLabel = null) =>
        EmailLayout.Render(
            title: title,
            body: body,
            greeting: greeting,
            details: detail,
            note: note,
            ctaUrl: ctaUrl,
            ctaLabel: ctaLabel);

    private string FormatSlot(BookingSlot slot)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, Zurich);
        var end = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc, Zurich);
        return $"{start:dddd, d. MMMM yyyy, HH:mm} – {end:HH:mm} Uhr";
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlContent)
    {
        var apiKey = config["Brevo:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogWarning("Brevo:ApiKey not configured — skipping email to {Email}", toEmail);
            return;
        }

        var payload = new
        {
            sender = new { name = SenderName, email = SenderEmail },
            to = new[] { new { name = toName, email = toEmail } },
            subject,
            htmlContent
        };

        var client = httpClientFactory.CreateClient("Brevo");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Brevo email to {Email} failed: {Status} {Body}", toEmail, response.StatusCode, body);
        }
    }
}
