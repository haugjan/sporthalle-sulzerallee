using System.Text;
using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

namespace SporthalleWeb.Infrastructure.PassivMitgliedschaft.Excel;

public sealed class AbaninjaCsvAdapter : IAbaninjaCsvPort
{
    private static readonly string[] Headers =
    [
        "Benutzer", "Kundennummer", "Unternehmensname", "Anrede",
        "Vorname", "Nachname", "E-Mail Adresse", "Webseite",
        "Telefon", "Mobiltelefon", "Strasse", "Hausnummer",
        "Zusatzfeld", "Adresszusatz", "PLZ", "Stadt", "Land",
        "Notizen", "Währung", "Kriterien",
        .. Enumerable.Range(1, 10).SelectMany(i => new[]
        {
            $"Mitarbeiter {i}", $"Mitarbeiter {i} Webseite",
            $"Mitarbeiter {i} Telefon", $"Mitarbeiter {i} Mobiltelefon",
            $"Mitarbeiter {i} E-Mail"
        })
    ];

    public byte[] ExportMembers(IReadOnlyList<PassivMitglied> members)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", Headers.Select(Q)));

        foreach (var m in members)
        {
            var notes = $"Feld Nr. {m.FieldNumber.Value} – {m.Level.DisplayName} – " +
                        $"CHF {m.Level.YearlyFee}.–/Jahr – " +
                        $"Anmeldung: {m.CreatedAt.ToLocalTime():dd.MM.yyyy}";
            if (!string.IsNullOrWhiteSpace(m.Notes))
                notes += $"\n{m.Notes}";

            var cols = new List<string>
            {
                "", $"PM{m.Id:D4}", "", "", m.FirstName, m.LastName,
                m.Email.Value, "", "", "",
                m.AddressLine, "", "", "",
                m.PostalCode, m.City, m.Country,
                notes, "CHF", "PassivMitglied"
            };
            cols.AddRange(Enumerable.Repeat("", 50));

            sb.AppendLine(string.Join(";", cols.Select(Q)));
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    private static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
}
