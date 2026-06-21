using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;

namespace SporthalleWeb.Infrastructure.Booking.Email;

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
        SendAsync(member.Email, FullName(member),
            "Ihr Anmelde-Link – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {FullName(member)}</p>
            <p>Klicken Sie auf den folgenden Link, um sich anzumelden (gültig 20 Minuten):</p>
            <p><a href="{magicLink}">{magicLink}</a></p>
            <p>Falls Sie diesen Link nicht angefordert haben, ignorieren Sie diese E-Mail.</p>
            """);

    public Task SendRegistrationConfirmationWithMagicLinkAsync(HallMember member, string magicLink) =>
        SendAsync(member.Email, FullName(member),
            "Registrierung bestätigt – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {FullName(member)}</p>
            <p>Ihre Registrierung war erfolgreich. Klicken Sie auf den folgenden Link, um sich anzumelden:</p>
            <p><a href="{magicLink}">{magicLink}</a></p>
            """);

    public Task SendPasswordResetAsync(HallMember member, string resetUrl) =>
        SendAsync(member.Email, FullName(member),
            "Passwort zurücksetzen – Sporthalle Sulzerallee",
            $"""
            <p>Guten Tag {FullName(member)}</p>
            <p>Klicken Sie auf den folgenden Link, um Ihr Passwort zurückzusetzen (gültig 24 Stunden):</p>
            <p><a href="{resetUrl}">{resetUrl}</a></p>
            <p>Falls Sie kein neues Passwort angefordert haben, ignorieren Sie diese E-Mail.</p>
            """);

    public Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Ihre Buchungsanfrage für <strong>{FormatSlot(slot)}</strong> ist bei uns eingegangen und wird geprüft.";
        return SendAsync(member.Email, FullName(member),
            "Buchungsanfrage erhalten – Sporthalle Sulzerallee",
            BuildEmail(
                title: "Buchungsanfrage erhalten",
                greeting: $"Guten Tag {FullName(member)}",
                body: body,
                detail: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Sie erhalten eine separate Bestätigung, sobald die Buchung genehmigt wurde." : null));
    }

    public Task SendAdminNewBookingNotificationAsync(BookingSlot slot, HallMember member) =>
        SendAsync(AdminEmail, SenderName,
            $"Neue Buchungsanfrage von {FullName(member)}",
            BuildEmail(
                title: "Neue Buchungsanfrage",
                greeting: "Hallo",
                body: $"Eine neue Buchungsanfrage ist eingegangen.",
                detail: $"Mieter: {FullName(member)} ({member.Email})<br>Zeitslot: {FormatSlot(slot)}<br>Anlass: {slot.Title}",
                ctaUrl: "https://www.sporthalle-sulzerallee.ch/umbraco",
                ctaLabel: "Zur Verwaltung"));

    public Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Ihre Buchung für <strong>{FormatSlot(slot)}</strong> wurde bestätigt.";
        return SendAsync(member.Email, FullName(member),
            "Buchung bestätigt – Sporthalle Sulzerallee",
            BuildEmail(
                title: "Buchung bestätigt",
                greeting: $"Guten Tag {FullName(member)}",
                body: body,
                detail: customEmailBody is null ? $"Anlass: {slot.Title}" : null,
                note: customEmailBody is null ? "Bei Fragen wenden Sie sich bitte an reservierung@sporthalle-sulzerallee.ch." : null));
    }

    public Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null)
    {
        var body = customEmailBody is not null
            ? System.Net.WebUtility.HtmlEncode(customEmailBody).Replace("\n", "<br>")
            : $"Leider können wir Ihre Buchungsanfrage für <strong>{FormatSlot(slot)}</strong> nicht bestätigen.";
        return SendAsync(member.Email, FullName(member),
            "Buchungsanfrage abgelehnt – Sporthalle Sulzerallee",
            BuildEmail(
                title: "Buchungsanfrage abgelehnt",
                greeting: $"Guten Tag {FullName(member)}",
                body: body,
                note: customEmailBody is null ? "Bitte kontaktieren Sie uns unter reservierung@sporthalle-sulzerallee.ch für weitere Informationen oder einen alternativen Termin." : null));
    }

    private static string FullName(HallMember m) =>
        $"{m.ContactFirstName} {m.ContactLastName}".Trim();

    private static string BuildEmail(
        string title, string greeting, string body,
        string? detail = null, string? note = null,
        string? ctaUrl = null, string? ctaLabel = null) => $"""
        <!DOCTYPE html>
        <html lang="de">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f4f4f4;font-family:'Helvetica Neue',Helvetica,Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f4;padding:40px 0;">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
                <!-- Header -->
                <tr>
                  <td style="background:#1a1a1a;padding:28px 40px;">
                    <span style="font-family:'Barlow Condensed',Arial,sans-serif;font-size:22px;font-weight:700;color:#ffffff;letter-spacing:0.04em;text-transform:uppercase;">Sporthalle Sulzerallee</span>
                  </td>
                </tr>
                <!-- Title bar -->
                <tr>
                  <td style="background:#EB504B;padding:16px 40px;">
                    <span style="font-family:'Barlow Condensed',Arial,sans-serif;font-size:20px;font-weight:700;color:#ffffff;letter-spacing:0.06em;text-transform:uppercase;">{title}</span>
                  </td>
                </tr>
                <!-- Body -->
                <tr>
                  <td style="padding:32px 40px;">
                    <p style="margin:0 0 16px;font-size:16px;color:#1a1a1a;">{greeting}</p>
                    <p style="margin:0 0 20px;font-size:15px;color:#333;line-height:1.6;">{body}</p>
                    {(detail != null ? $"<div style=\"background:#f8f8f8;border-left:3px solid #EB504B;padding:14px 18px;margin:0 0 20px;font-size:14px;color:#333;line-height:1.7;\">{detail}</div>" : "")}
                    {(note != null ? $"<p style=\"margin:0 0 20px;font-size:14px;color:#666;line-height:1.6;\">{note}</p>" : "")}
                    {(ctaUrl != null ? $"<p style=\"margin:24px 0 0;\"><a href=\"{ctaUrl}\" style=\"display:inline-block;background:#EB504B;color:#ffffff;font-family:'Barlow Condensed',Arial,sans-serif;font-size:15px;font-weight:700;text-transform:uppercase;letter-spacing:0.06em;text-decoration:none;padding:12px 28px;border-radius:4px;\">{ctaLabel ?? ctaUrl}</a></p>" : "")}
                  </td>
                </tr>
                <!-- Footer -->
                <tr>
                  <td style="background:#f8f8f8;border-top:1px solid #e8e8e8;padding:20px 40px;">
                    <p style="margin:0;font-size:12px;color:#888;line-height:1.6;">
                      Sporthalle Sulzerallee · Sulzerallee · 8404 Winterthur<br>
                      <a href="mailto:reservierung@sporthalle-sulzerallee.ch" style="color:#EB504B;text-decoration:none;">reservierung@sporthalle-sulzerallee.ch</a>
                    </p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

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
