using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using SporthalleWeb.Features.PassiveMembership.Registration;
using SporthalleWeb.Infrastructure.Shared;

namespace SporthalleWeb.Infrastructure.PassiveMembership;

public sealed class GraphPassiveMemberEmail(GraphMailClient graph) : IPassiveMemberEmail
{
    private const string SenderEmail = "noreply@sporthalle-sulzerallee.ch";
    private const string SenderName = "Sporthalle Sulzerallee";
    private const string BccEmail = "passivmitglieder@sporthalle-sulzerallee.ch";

    public async Task SendRegistrationConfirmationAsync(PassiveMember member)
    {
        var vipLabel = VipField.GetLabel(member.FieldNumber.Value);
        var fieldDesc = vipLabel != null
            ? $"Feld Nr. {member.FieldNumber.Value} ({vipLabel})"
            : $"Feld Nr. {member.FieldNumber.Value}";

        var details = $"Feld: {fieldDesc}\n" +
                      $"Stufe: {member.Level.DisplayName} ({member.Level.Key}) – CHF {member.Level.YearlyFee}.–/Jahr\n" +
                      $"Anmeldedatum: {member.CreatedAt:dd.MM.yyyy}";

        var htmlContent = EmailLayout.Render(
            title: "Passivmitgliedschaft bestätigt",
            body: "Herzlich willkommen bei der Sporthalle Sulzerallee! " +
                  $"Deine Anmeldung als Passivmitglied ({member.Level.DisplayName}) ist eingegangen. " +
                  $"Die erste Zahlung des Jahresbeitrags von CHF {member.Level.YearlyFee}.– erfolgt bequem per TWINT über den folgenden Link. " +
                  "Sobald deine Zahlung eingegangen ist, wird eine allfällig gewünschte Nennung auf dem Unihockeyfeld angezeigt.",
            greeting: $"Hallo {member.FirstName},",
            details: details,
            note: "Hast du direkt nach der Anmeldung bereits per TWINT bezahlt? Dann kannst du diesen Hinweis ignorieren.",
            ctaUrl: PaymentLink.ForField(member.FieldNumber.Value, member.Email.Value, $"{member.FirstName} {member.LastName}"),
            ctaLabel: "Jetzt per TWINT bezahlen");

        await graph.SendAsync(
            SenderEmail, SenderName,
            member.Email.Value, $"{member.FirstName} {member.LastName}",
            $"Willkommen als Passivmitglied – {fieldDesc}",
            htmlContent,
            BccEmail);
    }
}
