using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

namespace SporthalleWeb.Infrastructure.PassivMitgliedschaft.Email;

public class BrevoEmailAdapter : IEmailPort
{
    private readonly HttpClient _http;
    private readonly BrevoEmailOptions _opts;

    private static readonly string[] AdminBcc =
    [
        "bettina.zahnd@sporthalle-sulzerallee.ch",
        "matthias.lehner@sporthalle-sulzerallee.ch",
        "jan.haug@sporthalle-sulzerallee.ch"
    ];

    public BrevoEmailAdapter(HttpClient http, IOptions<BrevoEmailOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public async Task SendRegistrationConfirmationAsync(PassivMitglied member)
    {
        // Phase 2 ergänzt hier VipField.GetLabel() für fieldDesc
        var fieldDesc = $"Feld Nr. {member.FieldNumber.Value}";

        var details = $"Feld: {fieldDesc}\n" +
                      $"Stufe: {member.Level.DisplayName} ({member.Level.Key}) – CHF {member.Level.YearlyFee}.–/Jahr\n" +
                      $"Adresse: {member.AddressLine}, {member.PostalCode} {member.City}\n" +
                      $"Anmeldedatum: {member.CreatedAt:dd.MM.yyyy}";

        var payload = new
        {
            templateId = 1,
            to = new[] { new { email = member.Email.Value, name = $"{member.FirstName} {member.LastName}" } },
            bcc = AdminBcc.Select(e => new { email = e }).ToArray(),
            @params = new
            {
                SUBJECT   = $"Willkommen als Passivmitglied – {fieldDesc}",
                TITLE     = "Passivmitgliedschaft bestätigt",
                FIRSTNAME = member.FirstName,
                BODY      = $"Herzlich willkommen bei der Sporthalle Sulzerallee! " +
                             $"Deine Anmeldung als Passivmitglied ({member.Level.DisplayName}) ist eingegangen. " +
                             $"Du erhältst die Rechnung für den Jahresbeitrag in Kürze separat.",
                DETAILS   = details,
                CTA_URL   = "https://www.sporthalle-sulzerallee.ch",
                CTA_LABEL = "Zur Website"
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _opts.ApiKey);

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}
