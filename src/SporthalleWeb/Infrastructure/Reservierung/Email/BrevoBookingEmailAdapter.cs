using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Infrastructure.Reservierung.Email;

public sealed class BrevoBookingEmailAdapter(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<BrevoBookingEmailAdapter> logger) : IBookingEmailPort
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    private string AdminEmail => config["Brevo:AdminEmail"] ?? "admin@sporthalle-sulzerallee.ch";
    private string SenderEmail => config["Brevo:SenderEmail"] ?? "noreply@sporthalle-sulzerallee.ch";
    private string SenderName => "Sporthalle Sulzerallee";

    public Task SendMagicLinkAsync(HallMember member, string magicLink) =>
        SendAsync(member.Email, member.ContactPerson,
            "Ihr Anmelde-Link – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {member.ContactPerson}</p>
            <p>Klicken Sie auf den folgenden Link, um sich anzumelden (gültig 20 Minuten):</p>
            <p><a href="{magicLink}">{magicLink}</a></p>
            <p>Falls Sie diesen Link nicht angefordert haben, ignorieren Sie diese E-Mail.</p>
            """);

    public Task SendRegistrationConfirmationWithMagicLinkAsync(HallMember member, string magicLink) =>
        SendAsync(member.Email, member.ContactPerson,
            "Registrierung bestätigt – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {member.ContactPerson}</p>
            <p>Ihre Registrierung war erfolgreich. Klicken Sie auf den folgenden Link, um sich anzumelden:</p>
            <p><a href="{magicLink}">{magicLink}</a></p>
            """);

    public Task SendPasswordResetAsync(HallMember member, string resetUrl) =>
        SendAsync(member.Email, member.ContactPerson,
            "Passwort zurücksetzen – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {member.ContactPerson}</p>
            <p>Klicken Sie auf den folgenden Link, um Ihr Passwort zurückzusetzen (gültig 24 Stunden):</p>
            <p><a href="{resetUrl}">{resetUrl}</a></p>
            <p>Falls Sie kein neues Passwort angefordert haben, ignorieren Sie diese E-Mail.</p>
            """);

    public Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member) =>
        SendAsync(member.Email, member.ContactPerson,
            "Buchungsanfrage erhalten – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {member.ContactPerson}</p>
            <p>Ihre Buchungsanfrage für <strong>{FormatSlot(slot)}</strong> wurde erhalten und wird von uns geprüft.</p>
            <p>Sie erhalten eine separate Bestätigung sobald die Buchung genehmigt wurde.</p>
            """);

    public Task SendAdminNewBookingNotificationAsync(BookingSlot slot, HallMember member) =>
        SendAsync(AdminEmail, SenderName,
            $"Neue Buchungsanfrage von {member.ContactPerson}",
            $"""
            <p>Neue Buchungsanfrage:</p>
            <ul>
              <li>Mieter: {member.ContactPerson} ({member.Email})</li>
              <li>Zeitslot: {FormatSlot(slot)}</li>
              <li>Anlass: {slot.EventType ?? "-"}</li>
              <li>Gesamtpreis: CHF {slot.TotalPrice?.ToString("0.00") ?? "-"}</li>
            </ul>
            <p><a href="https://www.sporthalle-sulzerallee.ch/umbraco">Zur Verwaltung</a></p>
            """);

    public Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member) =>
        SendAsync(member.Email, member.ContactPerson,
            "Buchung bestätigt – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {member.ContactPerson}</p>
            <p>Ihre Buchung für <strong>{FormatSlot(slot)}</strong> wurde bestätigt.</p>
            <p>Gesamtbetrag: CHF {slot.TotalPrice?.ToString("0.00") ?? "-"}</p>
            <p>Bei Fragen wenden Sie sich bitte an uns.</p>
            """);

    public Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member) =>
        SendAsync(member.Email, member.ContactPerson,
            "Buchungsanfrage abgelehnt – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {member.ContactPerson}</p>
            <p>Leider können wir Ihre Buchungsanfrage für <strong>{FormatSlot(slot)}</strong> nicht bestätigen.</p>
            <p>Bitte kontaktieren Sie uns für weitere Informationen oder einen alternativen Termin.</p>
            """);

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
