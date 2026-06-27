namespace SporthalleWeb.Features.Booking.Configuration;

/// <summary>
/// Default, editable mail texts for the booking flow (Du-Form). Admins can override
/// the reservation and confirmation texts in the Konfiguration tab; these defaults
/// are used when nothing is configured. Placeholders: {Vorname}, {Name}, {Anlass},
/// {Datum}, {Von}, {Bis}.
/// </summary>
public static class BookingMailTemplates
{
    public const string ReservationDefault =
        "Hallo {Vorname}\n\n" +
        "vielen Dank für deine Anfrage! Wir haben deine Reservation für \"{Anlass}\" " +
        "am {Datum} von {Von} bis {Bis} Uhr erhalten und prüfen sie so schnell wie möglich.\n\n" +
        "Sobald deine Buchung bestätigt ist, bekommst du von uns eine separate E-Mail. " +
        "Bei Fragen sind wir unter reservation@sporthalle-sulzerallee.ch gerne für dich da.\n\n" +
        "Sportliche Grüsse\n" +
        "Dein Team der Sporthalle Sulzerallee";

    public const string ConfirmationDefault =
        "Hallo {Vorname}\n\n" +
        "gute Neuigkeiten: Deine Buchung für \"{Anlass}\" am {Datum} von {Von} bis {Bis} Uhr ist bestätigt.\n\n" +
        "Wir freuen uns auf dich! Falls sich etwas ändert oder du Fragen hast, " +
        "melde dich einfach unter reservation@sporthalle-sulzerallee.ch.\n\n" +
        "Sportliche Grüsse\n" +
        "Dein Team der Sporthalle Sulzerallee";

    public const string RejectionDefault =
        "Hallo {Vorname}\n\n" +
        "leider können wir deine Buchungsanfrage für \"{Anlass}\" am {Datum} von {Von} bis {Bis} Uhr " +
        "nicht bestätigen.\n\n" +
        "Melde dich gerne unter reservation@sporthalle-sulzerallee.ch, " +
        "wir finden zusammen einen passenden Alternativtermin.\n\n" +
        "Sportliche Grüsse\n" +
        "Dein Team der Sporthalle Sulzerallee";

    /// <summary>Replaces the placeholders in a template with the concrete values.</summary>
    public static string Apply(
        string template, string vorname, string name,
        string anlass, string datum, string von, string bis) =>
        template
            .Replace("{Vorname}", vorname)
            .Replace("{Name}", name)
            .Replace("{Anlass}", anlass)
            .Replace("{Datum}", datum)
            .Replace("{Von}", von)
            .Replace("{Bis}", bis);
}
