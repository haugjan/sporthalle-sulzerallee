# System-E-Mails

Alle System-Mails werden **direkt im Code gerendert** (kein Brevo-Template). Das
Design ist damit versioniert, im Git getrackt und an einer einzigen Stelle
gepflegt. Brevo wird nur noch als Versanddienst genutzt (`htmlContent` im
API-Aufruf), nicht mehr als Template-Verwalter.

## Single Source of Truth

`Infrastructure/Shared/EmailLayout.cs` rendert das gemeinsame Layout (Design an
die Homepage angelehnt: Dust-Rot `#EB504B`, Ink `#101010`, Manrope / Anton):

```csharp
EmailLayout.Render(
    title:    "Buchung bestätigt",
    body:     "Ihre Buchung wurde bestätigt.\nWir freuen uns auf Sie.",
    greeting: "Guten Tag Max Muster",   // optional
    details:  "Anlass: …\nZeit: …",     // optional (graue Box)
    note:     "Bei Fragen …",           // optional (gedämpfter Hinweis)
    ctaUrl:   "https://…",              // optional (Button)
    ctaLabel: "Zur Website");           // optional
```

`\n` in `body`/`details` wird zu `<br>`. Optionale Bloecke entfallen, wenn der
Wert leer ist. `body`/`details` duerfen auch einfaches HTML enthalten.

`docs/email/email-design-reference.html` ist die **Design-Referenz** (gleiche
Optik), nuetzlich zum Anschauen im Browser. Sie wird nicht mehr aktiv genutzt;
massgeblich ist `EmailLayout.cs`.

## Wer nutzt es

| Mail | Adapter / Methode |
|------|-------------------|
| Reservationsbestätigung | `BrevoBookingEmail.SendProvisionConfirmationToRenterAsync` |
| Buchungsbestätigung | `BrevoBookingEmail.SendBookingConfirmedToRenterAsync` |
| Buchungsabsage | `BrevoBookingEmail.SendBookingRejectedToRenterAsync` |
| Admin-Benachrichtigung (neue Anfrage) | `BrevoBookingEmail.SendAdminNewBookingNotificationAsync` |
| Passivmitglied-Anfrage / Bestätigung | `BrevoPassiveMemberEmail.SendRegistrationConfirmationAsync` |

Alle bauen ihr HTML über `EmailLayout.Render(...)` und senden es als
`htmlContent` an `POST https://api.brevo.com/v3/smtp/email`.

## BCC

BCC ist **kein** Template-Thema. Es wird pro Versand im API-Payload mitgegeben
(Feld `bcc`), genau wie `to` und `sender`:

```json
{
  "sender": { "name": "Sporthalle Sulzerallee", "email": "noreply@sporthalle-sulzerallee.ch" },
  "to":     [{ "email": "empfaenger@example.ch", "name": "Max Muster" }],
  "bcc":    [{ "email": "jan.haug@sporthalle-sulzerallee.ch" }],
  "subject": "Buchung bestätigt",
  "htmlContent": "<!DOCTYPE html> … (aus EmailLayout.Render) …"
}
```

Die Passivmitglied-Mail setzt die Admin-BCC-Adressen in `BrevoPassiveMemberEmail`.

## Brevo-Template ID 1

Wird nicht mehr verwendet. In Brevo kann es bei Bedarf geloescht werden; der Code
referenziert es nicht mehr.

## Logo

`EmailLayout.cs` referenziert das Logo absolut
(`https://app-sporthalle-sulzerallee.azurewebsites.net/img/sporthalle_sulzerallee_logo_neu.png`).
Bei eigener Domain dort anpassen.
