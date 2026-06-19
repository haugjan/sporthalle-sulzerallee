# Transaktionale E-Mail via Brevo senden

## Template

- **Name:** Transaktionale Mail
- **Template-ID:** `1`
- **Absender:** Sporthalle Sulzerallee `<jan.haug@sporthalle-sulzerallee.ch>`

## Template-Variablen

| Variable            | Beschreibung                              | Beispiel                              |
|---------------------|-------------------------------------------|---------------------------------------|
| `params.SUBJECT`    | E-Mail-Betreff                            | `Deine Buchung ist bestätigt`         |
| `params.TITLE`      | Grosser Titel im Header                   | `Buchung bestätigt`                   |
| `params.FIRSTNAME`  | Vorname des Empfängers                    | `Jan`                                 |
| `params.BODY`       | Haupttext                                 | `Vielen Dank für deine Buchung...`    |
| `params.DETAILS`    | Inhalt der Info-Box (Details/Buchungsinfo)| `Datum: ...\nZeit: ...\nHalle: ...`   |
| `params.CTA_URL`    | URL des Call-to-Action-Buttons            | `https://www.sporthalle-sulzerallee.ch` |
| `params.CTA_LABEL`  | Beschriftung des Buttons                  | `Zur Website`                         |

## Aufruf via Brevo API (PowerShell)

```powershell
$body = @{
    templateId = 1
    to = @(@{ email = "empfaenger@beispiel.ch"; name = "Vorname Nachname" })
    params = @{
        SUBJECT   = "Deine Buchung ist bestätigt"
        TITLE     = "Buchung bestätigt"
        FIRSTNAME = "Jan"
        BODY      = "Vielen Dank für deine Buchung bei der Sporthalle Sulzerallee."
        DETAILS   = "Datum: Samstag, 21. Juni 2026`nZeit: 14:00 – 16:00 Uhr`nHalle: Haupthalle"
        CTA_URL   = "https://www.sporthalle-sulzerallee.ch"
        CTA_LABEL = "Zur Website"
    }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod `
    -Uri "https://api.brevo.com/v3/smtp/email" `
    -Method POST `
    -Headers @{
        "api-key"      = $env:BREVO_API_KEY
        "Content-Type" = "application/json"
    } `
    -Body $body
```

## Aufruf via Brevo API (curl)

```bash
curl -X POST https://api.brevo.com/v3/smtp/email \
  -H "api-key: $BREVO_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "templateId": 1,
    "to": [{ "email": "empfaenger@beispiel.ch", "name": "Vorname Nachname" }],
    "params": {
      "SUBJECT":    "Deine Buchung ist bestätigt",
      "TITLE":      "Buchung bestätigt",
      "FIRSTNAME":  "Jan",
      "BODY":       "Vielen Dank für deine Buchung bei der Sporthalle Sulzerallee.",
      "DETAILS":    "Datum: Samstag, 21. Juni 2026\nZeit: 14:00 – 16:00 Uhr\nHalle: Haupthalle",
      "CTA_URL":    "https://www.sporthalle-sulzerallee.ch",
      "CTA_LABEL":  "Zur Website"
    }
  }'
```

## Hinweise

- Den API-Key nie im Code hardcoden. Stattdessen als Umgebungsvariable `BREVO_API_KEY` verwenden.
- Das Template kann in Brevo unter **E-Mail > Templates** eingesehen und bearbeitet werden.
- Den Sendestatus kann man über die Brevo-API abfragen:

```powershell
Invoke-RestMethod `
    -Uri "https://api.brevo.com/v3/smtp/statistics/events?email=empfaenger@beispiel.ch&limit=5" `
    -Method GET `
    -Headers @{ "api-key" = $env:BREVO_API_KEY }
```
