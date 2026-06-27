using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SporthalleWeb.Features.PassiveMembership.Registration;
using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.PassiveMembership;

public class BrevoPassiveMemberEmail(HttpClient http, IOptions<BrevoEmailOptions> opts) : IPassiveMemberEmail
{
    private readonly BrevoEmailOptions _opts = opts.Value;

    private static readonly string[] AdminBcc =
    [
        "bettina.zahnd@sporthalle-sulzerallee.ch",
        "matthias.lehner@sporthalle-sulzerallee.ch",
        "jan.haug@sporthalle-sulzerallee.ch"
    ];

    public async Task SendRegistrationConfirmationAsync(PassiveMember member)
    {
        var vipLabel = VipField.GetLabel(member.FieldNumber.Value);
        var fieldDesc = vipLabel != null
            ? $"Feld Nr. {member.FieldNumber.Value} ({vipLabel})"
            : $"Feld Nr. {member.FieldNumber.Value}";

        var details = $"Feld: {fieldDesc}\n" +
                      $"Stufe: {member.Level.DisplayName} ({member.Level.Key}) – CHF {member.Level.YearlyFee}.–/Jahr\n" +
                      $"Adresse: {member.AddressLine}, {member.PostalCode.Value} {member.City}\n" +
                      $"Anmeldedatum: {member.CreatedAt:dd.MM.yyyy}";

        var htmlContent = EmailLayout.Render(
            title: "Passivmitgliedschaft bestätigt",
            body: "Herzlich willkommen bei der Sporthalle Sulzerallee! " +
                  $"Deine Anmeldung als Passivmitglied ({member.Level.DisplayName}) ist eingegangen. " +
                  "Du erhältst die Rechnung für den Jahresbeitrag in Kürze separat.",
            greeting: $"Hallo {member.FirstName},",
            details: details,
            ctaUrl: "https://www.sporthalle-sulzerallee.ch",
            ctaLabel: "Zur Website");

        var payload = new
        {
            sender = new { name = "Sporthalle Sulzerallee", email = "noreply@sporthalle-sulzerallee.ch" },
            to = new[] { new { email = member.Email.Value, name = $"{member.FirstName} {member.LastName}" } },
            bcc = AdminBcc.Select(e => new { email = e }).ToArray(),
            subject = $"Willkommen als Passivmitglied – {fieldDesc}",
            htmlContent
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Add("api-key", _opts.ApiKey);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
