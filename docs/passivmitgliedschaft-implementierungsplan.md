# Implementierungsplan: Passivmitgliedschaft

Erstellt: 2026-06-17

---

## 1. Übersicht & Ziele

Die neue Seite "Passivmitgliedschaft" ermöglicht es Interessenten, sich als Passivmitglied des
Vereins Sporthalle Sulzerallee anzumelden. Kernidee: man wählt symbolisch einen Quadratmeter
des Unihockey-Hallenbodens (1 von 300 Feldern), wählt eine Mitgliedsstufe und gibt seine
persönlichen Daten an. Nach der Anmeldung erhält man eine Bestätigungs-E-Mail via Brevo.
Administratoren verwalten die Mitglieder über einen geschützten Bereich.

### Mitgliedsstufen

| Stufe | Jahresbeitrag | Benefits |
|-------|--------------|---------|
| Cüpli-Chnebler (Gold) | CHF 200 | 1m² Boden symbolisch, Zugang offene Sonntage (Familie), jährlicher Sponsor-Event, Newsletter |
| Chnebler (Silber) | CHF 100 | 1m² Boden symbolisch, Zugang offene Sonntage (Familie), Newsletter |
| Hallenbodenbesitzer (Bronze) | CHF 50 | 1m² Boden symbolisch, Newsletter |

### Getroffene Entscheidungen

| Thema | Entscheid |
|-------|-----------|
| E-Mail-Provider | Brevo (API, Template ID 1, API-Key als `BREVO_API_KEY` Env-Variable) |
| Admin-BCC | bettina.zahnd@, matthias.lehner@, jan.haug@ (alle @sporthalle-sulzerallee.ch) |
| Zahlungsdetails in E-Mail | Keine. Die Rechnung kommt separat. E-Mail ist reine Bestätigung. |
| CAPTCHA | Registrierungsformular ist mit Cloudflare Turnstile gesichert (datenschutzfreundlich, kein Cookie, kostenlos) |
| Felder Torraum / Kreise | VIP-Felder: werden angezeigt und können gewählt werden, aber sind visuell hervorgehoben und namentlich bezeichnet (z.B. "Torraum", "Anspielkreis") |
| Live-Zähler | Ja: "X von 300 Feldern belegt" wird auf der Seite angezeigt |
| Page-Header-Bild | Keines vorhanden; Header zeigt nur dunklen Hintergrund mit Noise-Effekt |

### Abgrenzung

- Keine direkte Zahlungsintegration. Die Jahresrechnung wird separat verschickt (ausserhalb dieser Anwendung).
- Admin-Bereich: per Umbraco-Login geschützte Razor Page, kein separates Backoffice-Extension-Plugin.

---

## 2. Architektur

### 2.1 Hexagonale Architektur (Ports & Adapters)

Der Kern (Domain + Application) kennt keine Infrastrukturdetails. Alles, was nach aussen geht
(DB, E-Mail, Excel), wird über Ports (Interfaces im Domain-Kern) abstrahiert. Adapter-Klassen
in der Infrastructure-Schicht implementieren diese Ports.

```
                     ┌──────────────────────────────────────┐
  HTTP-Request ─────►│  Presentation (Inbound Adapter)       │
                     │  Controller / Razor Pages / Views     │
                     └──────────────────┬───────────────────┘
                                        │ ruft auf
                     ┌──────────────────▼───────────────────┐
                     │  Application Layer (Use Cases)        │
                     │  RegisterMemberUseCase                │
                     │  GetFieldStatusesQuery                │
                     │  AdminService                         │
                     └──────────────────┬───────────────────┘
                                        │ nutzt Ports
                     ┌──────────────────▼───────────────────┐
                     │  Domain (Kern, keine Abhängigkeiten)  │
                     │  PassivMitglied (Aggregate Root)      │
                     │  FieldNumber, MembershipLevel,        │
                     │  MemberEmail (Value Objects)          │
                     │  IPassivMitgliederRepository (Port)   │
                     │  IEmailPort (Port)                    │
                     │  IExcelPort (Port)                    │
                     └──────────────────┬───────────────────┘
                                        │ implementiert durch
                     ┌──────────────────▼───────────────────┐
                     │  Infrastructure (Outbound Adapters)   │
                     │  PassivMitgliederRepository (PetaPoco)│
                     │  BrevoEmailAdapter (HttpClient)       │
                     │  ClosedXmlExcelAdapter                │
                     │  PassivMitgliederMigration            │
                     └──────────────────────────────────────┘
```

### 2.2 DDD (pragmatisch, nicht dogmatisch)

- **Aggregate Root:** `PassivMitglied` kapselt Zustandsänderungen (`MarkAsPaid()`, `UpdateNotes()`).
- **Value Objects:** `FieldNumber`, `MembershipLevel`, `MemberEmail` mit eingebetteter Validierungslogik.
- **Domain Events:** Einfache Records (`MemberRegisteredEvent`). Kein Event-Bus, kein Mediator.
- **Repository Port:** Interface im Domain-Kern definiert, Adapter in Infrastructure implementiert.

---

## 3. Dateistruktur

```
src/SporthalleWeb/
├── Domain/PassivMitgliedschaft/
│   ├── PassivMitglied.cs                       Aggregate Root
│   ├── FieldNumber.cs                          Value Object
│   ├── MembershipLevel.cs                      Value Object / Enum-Wrapper
│   ├── MemberEmail.cs                          Value Object
│   ├── VipField.cs                             Definiert benannte Sonderfelder (Torraum etc.)
│   ├── DomainException.cs
│   ├── Events/
│   │   └── MemberRegisteredEvent.cs
│   └── Ports/
│       ├── IPassivMitgliederRepository.cs
│       ├── IEmailPort.cs
│       └── IExcelPort.cs
│
├── Application/PassivMitgliedschaft/
│   ├── RegisterMemberUseCase.cs
│   ├── RegisterMemberCommand.cs
│   ├── GetFieldStatusesQuery.cs
│   ├── FieldStatusDto.cs
│   └── AdminService.cs
│
├── Infrastructure/PassivMitgliedschaft/
│   ├── Persistence/
│   │   ├── PassivMitgliederRepository.cs
│   │   ├── PassivMitgliedDbRecord.cs
│   │   └── PassivMitgliederMigration.cs
│   ├── Email/
│   │   ├── BrevoEmailAdapter.cs
│   │   └── BrevoEmailOptions.cs
│   ├── Excel/
│   │   └── ClosedXmlExcelAdapter.cs
│   └── Composition/
│       └── PassivMitgliederComposer.cs
│
└── Presentation/PassivMitgliedschaft/
    ├── Controllers/
    │   └── PassivMitgliederController.cs
    ├── Pages/
    │   ├── PassivMitgliederAdmin.cshtml
    │   └── PassivMitgliederAdmin.cshtml.cs
    └── Dtos/
        ├── RegisterMemberRequest.cs
        └── FieldStatusResponse.cs

Views/
└── PassivMitgliedschaft.cshtml             Umbraco Template (NEU)
    Home.cshtml                             Teaser-Abschnitt (GEÄNDERT)

uSync/v17/
├── ContentTypes/
│   ├── passivmitgliedschaft.config         NEU
│   └── homepage.config                     GEÄNDERT (Structure erweitert)
└── Templates/
    └── passivmitgliedschaft.config         NEU

wwwroot/
├── css/passivmitglied.css                  NEU
├── js/passivmitglied.js                    NEU
└── media/unihockey-boden.svg               NEU
```

Geänderte bestehende Dateien: `appsettings.json`, `SporthalleWeb.csproj`

---

## 4. Domain-Schicht

### 4.1 Aggregate Root `PassivMitglied`

```csharp
public sealed class PassivMitglied
{
    public int Id { get; private set; }
    public FieldNumber FieldNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string AddressLine { get; private set; }   // Strasse + Hausnummer in einem Feld
    public string PostalCode { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }        // immer "Schweiz"
    public MemberEmail Email { get; private set; }
    public MembershipLevel Level { get; private set; }
    public bool ShowNameOnFloor { get; private set; }
    public string? DisplayName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }

    private PassivMitglied() { }

    public static PassivMitglied Register(
        FieldNumber fieldNumber, string firstName, string lastName,
        string addressLine, string postalCode, string city,
        MemberEmail email, MembershipLevel level,
        bool showNameOnFloor, string? displayName)
    {
        if (showNameOnFloor && string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Anzeigename erforderlich, wenn Name sichtbar sein soll.");

        return new PassivMitglied
        {
            FieldNumber = fieldNumber,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            AddressLine = addressLine.Trim(),
            PostalCode = postalCode.Trim(),
            City = city.Trim(),
            Country = "Schweiz",
            Email = email,
            Level = level,
            ShowNameOnFloor = showNameOnFloor,
            DisplayName = showNameOnFloor ? displayName!.Trim() : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsPaid() => PaidAt = DateTime.UtcNow;
    public void UpdateNotes(string? notes) => Notes = notes?.Trim();
}
```

### 4.2 Value Objects

```csharp
public record FieldNumber
{
    public int Value { get; }
    public FieldNumber(int value)
    {
        if (value < 1 || value > 300)
            throw new DomainException($"Feldnummer muss zwischen 1 und 300 liegen (war: {value}).");
        Value = value;
    }
}

public record MemberEmail
{
    public string Value { get; }
    public MemberEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new DomainException("Ungültige E-Mail-Adresse.");
        Value = value.Trim().ToLowerInvariant();
    }
}

public record MembershipLevel
{
    public static readonly MembershipLevel Bronze = new("Hallenbodenbesitzer", "Bronze", 50);
    public static readonly MembershipLevel Silber = new("Chnebler", "Silber", 100);
    public static readonly MembershipLevel Gold   = new("Cüpli-Chnebler", "Gold",   200);

    public string DisplayName { get; }
    public string Key { get; }
    public decimal YearlyFee { get; }

    private MembershipLevel(string displayName, string key, decimal fee)
    {
        DisplayName = displayName; Key = key; YearlyFee = fee;
    }

    public static MembershipLevel FromKey(string key) => key switch
    {
        "Bronze" => Bronze,
        "Silber" => Silber,
        "Gold"   => Gold,
        _        => throw new DomainException($"Unbekannte Mitgliedsstufe: {key}")
    };
}
```

### 4.3 VIP-Felder

Felder im Torraum, Anspielkreis und an Anspielpunkten erhalten eine besondere Bezeichnung.
Das SVG-Grid ist 20 Spalten × 15 Zeilen = 300 Felder. Zellgrösse: 40 × 26.67 px (Spielfeld
800×400 px ab Offset 20,20). Feldnummer = (row × 20) + col + 1, 0-basiert.

```csharp
public static class VipField
{
    // Torraum links: SVG x=77-157, y=170-270 → Relativkoord. x=57-137, y=150-250
    // → col 1-3, row 5-9 (0-basiert)
    private static readonly HashSet<int> GoalCreaseLeft = ComputeRange(cols: (1,3), rows: (5,9));

    // Torraum rechts: gespiegelt → col 16-18, row 5-9
    private static readonly HashSet<int> GoalCreaseRight = ComputeRange(cols: (16,18), rows: (5,9));

    // Anspielkreis Mitte: SVG cx=420, cy=220, r=60 → Relativmitte (400,200)
    // → col 8-11, row 5-9 (Näherung)
    private static readonly HashSet<int> CenterCircle = ComputeRange(cols: (8,11), rows: (5,9));

    // Anspielpunkte (einzelne Felder): links (col 1, row 0/13), mitte (col 9, row 0/13), rechts (col 18, row 0/13)
    private static readonly HashSet<int> FaceOffSpots = new() { 2, 182, 181, 200, 20, 199, 199 }; // wird exakt berechnet

    public static string? GetLabel(int fieldNumber) =>
        GoalCreaseLeft.Contains(fieldNumber)   ? "Torraum" :
        GoalCreaseRight.Contains(fieldNumber)  ? "Torraum" :
        CenterCircle.Contains(fieldNumber)     ? "Anspielkreis" :
        FaceOffSpots.Contains(fieldNumber)     ? "Anspielpunkt" :
        null;

    public static bool IsVip(int fieldNumber) => GetLabel(fieldNumber) != null;

    private static HashSet<int> ComputeRange((int from, int to) cols, (int from, int to) rows)
    {
        var result = new HashSet<int>();
        for (var r = rows.from; r <= rows.to; r++)
            for (var c = cols.from; c <= cols.to; c++)
                result.Add(r * 20 + c + 1);
        return result;
    }
}
```

VIP-Felder sind regulär wählbar, werden aber im SVG-Overlay anders eingefärbt und tragen
ein Tooltip-Label. In Phase 2 kann hier ein Aufpreis oder eine andere Stufe eingeführt werden.

### 4.4 Outbound Ports

```csharp
public interface IPassivMitgliederRepository
{
    Task<bool> IsFieldTakenAsync(FieldNumber field);
    Task<PassivMitglied> SaveAsync(PassivMitglied member);
    Task<IReadOnlyList<PassivMitglied>> GetAllAsync();
    Task<PassivMitglied?> FindByIdAsync(int id);
    Task UpdateAsync(PassivMitglied member);
    Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync();
}

public interface IEmailPort
{
    Task SendRegistrationConfirmationAsync(PassivMitglied member);
}

public interface IExcelPort
{
    byte[] ExportMembers(IReadOnlyList<PassivMitglied> members);
}

public interface IAbaninjaCsvPort
{
    byte[] ExportMembers(IReadOnlyList<PassivMitglied> members);
}

public interface ICaptchaPort
{
    Task<bool> VerifyAsync(string token, string remoteIp);
}
```

---

## 5. Application-Schicht

### 5.1 `RegisterMemberUseCase`

```csharp
public sealed class RegisterMemberUseCase(
    IPassivMitgliederRepository repo, IEmailPort email)
{
    public async Task<PassivMitglied> ExecuteAsync(RegisterMemberCommand cmd)
    {
        var fieldNumber = new FieldNumber(cmd.FieldNumber);

        if (await repo.IsFieldTakenAsync(fieldNumber))
            throw new FieldAlreadyTakenException(fieldNumber);

        var member = PassivMitglied.Register(
            fieldNumber, cmd.FirstName, cmd.LastName, cmd.Address,
            new MemberEmail(cmd.Email), MembershipLevel.FromKey(cmd.LevelKey),
            cmd.ShowNameOnFloor, cmd.DisplayName);

        await repo.SaveAsync(member);
        await email.SendRegistrationConfirmationAsync(member);  // nach dem Speichern

        return member;
    }
}

public record RegisterMemberCommand(
    int FieldNumber, string FirstName, string LastName,
    string Address, string Email, string LevelKey,
    bool ShowNameOnFloor, string? DisplayName, bool Consent);
```

### 5.2 `GetFieldStatusesQuery`

```csharp
public sealed class GetFieldStatusesQuery(IPassivMitgliederRepository repo)
{
    public async Task<FieldStatusesResult> ExecuteAsync()
    {
        var occupied = await repo.GetOccupiedFieldsAsync();
        var fields = occupied
            .Select(f => new FieldStatusDto(f.Field.Value, f.DisplayName, VipField.GetLabel(f.Field.Value)))
            .ToList();
        return new FieldStatusesResult(fields, TotalFields: 300);
    }
}

public record FieldStatusDto(int FieldNumber, string? DisplayName, string? VipLabel);
public record FieldStatusesResult(IReadOnlyList<FieldStatusDto> OccupiedFields, int TotalFields);
```

### 5.3 `AdminService`

```csharp
public sealed class AdminService(IPassivMitgliederRepository repo, IExcelPort excel)
{
    public async Task MarkAsPaidAsync(int memberId)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.MarkAsPaid();
        await repo.UpdateAsync(member);
    }

    public async Task UpdateNotesAsync(int memberId, string? notes)
    {
        var member = await repo.FindByIdAsync(memberId)
            ?? throw new MemberNotFoundException(memberId);
        member.UpdateNotes(notes);
        await repo.UpdateAsync(member);
    }

    public async Task<byte[]> ExportAsync()
        => excel.ExportMembers(await repo.GetAllAsync());
}
```

---

## 6. Infrastructure-Schicht

### 6.1 Datenbankschema

```sql
CREATE TABLE PassivMitglieder (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    FieldNumber     INT NOT NULL UNIQUE,
    FirstName       NVARCHAR(100) NOT NULL,
    LastName        NVARCHAR(100) NOT NULL,
    AddressLine     NVARCHAR(300) NOT NULL,   -- Strasse + Hausnummer
    PostalCode      NVARCHAR(20)  NOT NULL,
    City            NVARCHAR(100) NOT NULL,
    Country         NVARCHAR(100) NOT NULL DEFAULT 'Schweiz',
    Email           NVARCHAR(200) NOT NULL,
    MembershipLevel NVARCHAR(20)  NOT NULL,   -- 'Bronze' | 'Silber' | 'Gold'
    ShowNameOnFloor BIT           NOT NULL DEFAULT 0,
    DisplayName     NVARCHAR(200) NULL,
    CreatedAt       DATETIME      NOT NULL,
    PaidAt          DATETIME      NULL,
    Notes           NVARCHAR(MAX) NULL
)
```

Migration läuft automatisch beim App-Start via `IMigrationPlan` + `IComposer`.

### 6.2 Brevo E-Mail-Adapter

Der Adapter implementiert `IEmailPort` via Brevo REST API (kein SMTP). Das bestehende
Template mit ID 1 ("Transaktionale Mail") wird wiederverwendet.

```csharp
public class BrevoEmailAdapter(HttpClient http, IOptions<BrevoEmailOptions> opts) : IEmailPort
{
    private static readonly string[] AdminBcc =
    [
        "bettina.zahnd@sporthalle-sulzerallee.ch",
        "matthias.lehner@sporthalle-sulzerallee.ch",
        "jan.haug@sporthalle-sulzerallee.ch"
    ];

    public async Task SendRegistrationConfirmationAsync(PassivMitglied member)
    {
        var vipLabel = VipField.GetLabel(member.FieldNumber.Value);
        var fieldDesc = vipLabel != null
            ? $"Feld Nr. {member.FieldNumber.Value} ({vipLabel})"
            : $"Feld Nr. {member.FieldNumber.Value}";

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
        request.Headers.Add("api-key", opts.Value.ApiKey);

        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}

public class BrevoEmailOptions
{
    public string ApiKey { get; set; } = "";
}
```

Konfiguration in `appsettings.json`:
```json
"Brevo": {
  "ApiKey": ""
}
```

`appsettings.Development.json` nutzt denselben Key (Dev-Account bei Brevo oder leer lassen für
lokales Testen mit einem Log-Only-Stub). Der API-Key wird in Azure als
App Service Environment Variable `Brevo__ApiKey` gesetzt.

### 6.3 AbaNinja-CSV-Adapter

Der Export erzeugt eine Semicolon-CSV-Datei im AbaNinja-Kontakte-Importformat, die direkt
unter "Kontakte > Importieren" in AbaNinja hochgeladen werden kann.

**Format:** Semikolon-getrennt, Felder in doppelten Anführungszeichen, UTF-8 mit BOM
(damit Excel und AbaNinja die Umlaute korrekt lesen).

**Spalten-Mapping:**

| AbaNinja-Spalte | Quelle |
|----------------|--------|
| `Benutzer` | leer (AbaNinja setzt den eigenen User) |
| `Kundennummer` | `PM` + Id.ToString("D4") (z.B. `PM0042`) |
| `Unternehmensname` | leer |
| `Anrede` | leer (nicht erhoben) |
| `Vorname` | `FirstName` |
| `Nachname` | `LastName` |
| `E-Mail Adresse` | `Email.Value` |
| `Webseite` | leer |
| `Telefon` | leer |
| `Mobiltelefon` | leer |
| `Strasse` | `AddressLine` (Strasse + Hausnummer kombiniert) |
| `Hausnummer` | leer |
| `Zusatzfeld` | leer |
| `Adresszusatz` | leer |
| `PLZ` | `PostalCode` |
| `Stadt` | `City` |
| `Land` | `Country` (immer "Schweiz") |
| `Notizen` | `"Feld Nr. {X} – {Level.DisplayName} – CHF {YearlyFee}.–/Jahr – Anmeldung: {CreatedAt:dd.MM.yyyy}"` + interne Notes wenn vorhanden |
| `Währung` | `CHF` |
| `Kriterien` | `Passivmitglied` |
| `Mitarbeiter 1`…`10` | alle leer |

```csharp
public class AbaninjaCsvAdapter : IAbaninjaCsvPort
{
    private static readonly string[] Headers =
    [
        "Benutzer", "Kundennummer", "Unternehmensname", "Anrede",
        "Vorname", "Nachname", "E-Mail Adresse", "Webseite",
        "Telefon", "Mobiltelefon", "Strasse", "Hausnummer",
        "Zusatzfeld", "Adresszusatz", "PLZ", "Stadt", "Land",
        "Notizen", "Währung", "Kriterien",
        // Mitarbeiter 1-10 (je 5 Felder) = 50 weitere leere Spalten
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
                        $"Anmeldung: {m.CreatedAt:dd.MM.yyyy}";
            if (!string.IsNullOrWhiteSpace(m.Notes))
                notes += $"\n{m.Notes}";

            var cols = new List<string>
            {
                "",                                // Benutzer
                $"PM{m.Id:D4}",                   // Kundennummer
                "", "", m.FirstName, m.LastName,   // Unternehmensname, Anrede, Vorname, Nachname
                m.Email.Value, "", "", "",          // E-Mail, Webseite, Telefon, Mobiltelefon
                m.AddressLine, "", "", "",          // Strasse, Hausnummer, Zusatzfeld, Adresszusatz
                m.PostalCode, m.City, m.Country,   // PLZ, Stadt, Land
                notes, "CHF", "Passivmitglied"    // Notizen, Währung, Kriterien
            };
            cols.AddRange(Enumerable.Repeat("", 50)); // Mitarbeiter 1-10

            sb.AppendLine(string.Join(";", cols.Select(Q)));
        }

        // UTF-8 mit BOM damit AbaNinja und Excel Umlaute korrekt lesen
        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    private static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
}
```

### 6.4 Excel-Adapter (ClosedXML) — internes Verwaltungs-Export

NuGet: `ClosedXML`. Dient der internen Verwaltung mit Bezahlstatus und Notizen.

Spalten: Nr. | Feld-Nr. | VIP-Label | Stufe | CHF/Jahr | Vorname | Nachname | Adresse | PLZ | Stadt | E-Mail | Angemeldet am | Bezahlt am | Notizen

### 6.5 CAPTCHA-Adapter (Cloudflare Turnstile)

Cloudflare Turnstile ist kostenlos, setzt kein Cookie, und ist DSGVO-konformer als
Google reCAPTCHA. Es gibt eine invisible- und eine managed-Variante (kurze Checkbox).

**Adapter:**

```csharp
public class TurnstileCaptchaAdapter(HttpClient http, IOptions<TurnstileOptions> opts) : ICaptchaPort
{
    public async Task<bool> VerifyAsync(string token, string remoteIp)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"]   = opts.Value.SecretKey,
            ["response"] = token,
            ["remoteip"] = remoteIp
        });
        var response = await http.PostAsync(
            "https://challenges.cloudflare.com/turnstile/v0/siteverify", form);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        return result.GetProperty("success").GetBoolean();
    }
}

public class TurnstileOptions
{
    public string SiteKey   { get; set; } = "";
    public string SecretKey { get; set; } = "";
}
```

Konfiguration in `appsettings.json`:
```json
"Turnstile": {
  "SiteKey":   "",
  "SecretKey": ""
}
```

Azure App Service Environment Variables: `Turnstile__SiteKey`, `Turnstile__SecretKey`.

Die Verifizierung findet im **Controller** statt (Presentation-Concern, nicht Domain):

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterMemberRequest req)
{
    var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
    if (!await _captcha.VerifyAsync(req.CaptchaToken, ip))
        return BadRequest(new { error = "captcha_failed" });

    // ... weiter mit RegisterMemberUseCase
}
```

### 6.4 Excel-Adapter (ClosedXML)

NuGet: `ClosedXML`

Spalten: Nr. | Feld-Nr. | VIP-Label | Stufe | CHF/Jahr | Vorname | Nachname | Adresse | E-Mail | Angemeldet am | Bezahlt am | Notizen

### 6.5 DI-Registrierung (`PassivMitgliederComposer`)

```csharp
public class PassivMitgliederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<PassivMitgliederMigrationComponent>();

        builder.Services.Configure<BrevoEmailOptions>(
            builder.Config.GetSection("Brevo"));

        builder.Services.AddHttpClient<BrevoEmailAdapter>();
        builder.Services.AddScoped<IEmailPort, BrevoEmailAdapter>();
        builder.Services.Configure<TurnstileOptions>(builder.Config.GetSection("Turnstile"));
        builder.Services.AddHttpClient<TurnstileCaptchaAdapter>();
        builder.Services.AddScoped<ICaptchaPort, TurnstileCaptchaAdapter>();
        builder.Services.AddScoped<IPassivMitgliederRepository, PassivMitgliederRepository>();
        builder.Services.AddSingleton<IExcelPort, ClosedXmlExcelAdapter>();
        builder.Services.AddSingleton<IAbaninjaCsvPort, AbaninjaCsvAdapter>();

        builder.Services.AddScoped<RegisterMemberUseCase>();
        builder.Services.AddScoped<GetFieldStatusesQuery>();
        builder.Services.AddScoped<AdminService>();
    }
}
```

---

## 7. API-Endpunkte (`PassivMitgliederController`)

```
GET  /api/passivmitglieder/felder
     → { occupiedFields: [{fieldNumber, displayName, vipLabel}], totalFields: 300, occupiedCount: N }
     Öffentlich, für SVG + Live-Zähler

POST /api/passivmitglieder/register
     Body: RegisterMemberRequest
     → 200 OK | 409 Conflict (Feld belegt) | 400 Bad Request

POST /api/passivmitglieder/{id}/paid          [UmbracoAdminAuthorize]
     → 204 No Content

POST /api/passivmitglieder/{id}/notes         [UmbracoAdminAuthorize]
     Body: { "notes": "..." }
     → 204 No Content

GET  /api/passivmitglieder/admin/members      [UmbracoAdminAuthorize]
     → PassivMitglied[] vollständig

GET  /api/passivmitglieder/admin/export/excel     [UmbracoAdminAuthorize]
     → application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
     Dateiname: Passivmitglieder_YYYY-MM-DD.xlsx

GET  /api/passivmitglieder/admin/export/abaninja  [UmbracoAdminAuthorize]
     → text/csv; charset=utf-8-bom
     Dateiname: Passivmitglieder_AbaNinja_YYYY-MM-DD.csv
```

Der `RegisterMemberRequest` enthält zusätzlich ein `CaptchaToken`-Feld. Der Controller
verifiziert dieses zuerst via `ICaptchaPort`, bevor der Use Case aufgerufen wird. Bei
Fehlschlag → `400 Bad Request` mit `{ "error": "captcha_failed" }`.

Server-seitige Validierung des `RegisterMemberRequest`:
- `CaptchaToken` vorhanden + Turnstile-Verifikation erfolgreich
- `FieldNumber` 1–300
- `LevelKey` in { "Bronze", "Silber", "Gold" }
- `Email` valides Format
- `FirstName`, `LastName`, `Address` nicht leer
- `Consent` muss `true` sein
- `DisplayName` Pflicht wenn `ShowNameOnFloor = true`

---

## 8. Frontend

### 8.1 SVG-Bodenplan mit Grid

`unihockey-boden.svg` wird inline in das Cshtml-Template gerendert. JavaScript legt
ein interaktives `<g>`-Element als Grid-Overlay über das SVG.

**Grid-Konfiguration (konstant in JS):**
```js
const GRID = {
  cols: 20, rows: 15,             // 300 Felder
  fieldX: 20, fieldY: 20,         // SVG-Offset Spielfeld
  cellW: 40, cellH: 800/15        // ≈40 × 26.67 px
};
```

**Farbcodierung:**

| Zustand | Farbe |
|---------|-------|
| Verfügbar | `rgba(255,255,255,0.08)` |
| Hover (verfügbar) | `rgba(255,255,255,0.25)` |
| VIP (verfügbar) | `rgba(255,215,0,0.15)` + goldener Rahmen |
| VIP (belegt) | `rgba(255,215,0,0.5)` |
| Belegt, anonym | `rgba(235,80,75,0.75)` |
| Belegt, mit Name | `rgba(235,80,75,0.9)` + Namelabel |
| Ausgewählt | `rgba(255,215,0,0.65)` |

VIP-Felder erhalten zusätzlich ein Tooltip mit dem Label ("Torraum", "Anspielkreis", etc.).

**Beim Laden:**
1. `GET /api/passivmitglieder/felder` abrufen
2. Belegte Felder einfärben (mit oder ohne Name)
3. VIP-Felder mittels `vipLabel`-Feld aus Response kennzeichnen
4. Live-Zähler aktualisieren: "X von 300 Feldern belegt"

**Klick auf freies Feld:** Wizard öffnen mit vorausgewählter Feldnummer.
**Touch-Support:** `touchstart`-Event zusätzlich zu `click` für Mobile.

### 8.2 Multi-Step-Wizard (Modal, Vanilla JS)

**Schritt 1 – Feld bestätigen:**
```
Feld Nr. 42 ausgewählt   [evtl. VIP-Label: "Anspielkreis"]
Hinweis: "Der Boden ist symbolisch – der physische Boden
          verbleibt im Eigentum des Vereins."
[Weiter]
```

**Schritt 2 – Mitgliedsstufe:**
```
Auswählbare Karten:
  Bronze – Hallenbodenbesitzer    CHF 50/Jahr   [Benefits]
  Silber – Chnebler               CHF 100/Jahr  [Benefits]
  Gold   – Cüpli-Chnebler         CHF 200/Jahr  [Benefits]
[Zurück] [Weiter]
```

**Schritt 3 – Persönliche Daten:**
```
Vorname*        [____________]   Nachname*  [____________]
Strasse + Nr.*  [________________________________]
PLZ*  [______]  Ort*           [____________________]
E-Mail*         [________________________________]
[Zurück] [Weiter]
```
Adresse ist aufgeteilt, damit der AbaNinja-Export direkt importierbar ist (PLZ/Stadt als
eigene CSV-Spalten). Land ist immer "Schweiz" und wird nicht abgefragt.

**Schritt 4 – Namensanzeige:**
```
Möchtest du deinen Namen auf dem Bodenplan sehen?
( ) Ja → Anzeigename: [____] (z.B. "Max M.", "Familie Meier")
(•) Nein, anonym bleiben
[Zurück] [Weiter]
```

**Schritt 5 – Zusammenfassung, CAPTCHA & Zustimmung:**
```
Feld-Nr.:   42  Stufe: Chnebler (Silber) – CHF 100.–/Jahr
Name:       Max Mustermann
Adresse:    Musterstrasse 1, 8400 Winterthur (Schweiz)
E-Mail:     max@example.com
Anzeige:    "Max M." (sichtbar auf dem Bodenplan)

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Der Hallenboden ist SYMBOLISCH. "Besitzer:in eines
Quadratmeters" ist eine Metapher für die Mitgliedschaft.
Der physische Boden verbleibt im Eigentum des Vereins.
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

☐ Ich möchte Passivmitglied werden und erkenne die
  jährliche Beitragspflicht (bis auf Widerruf) an.*

[Cloudflare Turnstile Widget]   ← erscheint automatisch, kein Klick nötig

[Zurück] [Jetzt anmelden]
```

**Schritt 6 – Bestätigung:**
```
✓ Vielen Dank, Max!
Du erhältst in Kürze eine Bestätigung an max@example.com.
Die Rechnung für den Jahresbeitrag wird separat zugestellt.
[Schliessen]
```

Nach dem Schliessen: SVG und Zähler ohne Reload aktualisieren (das neue Feld einzeichnen).

### 8.3 Live-Zähler

```html
<p class="field-counter">
  <span id="fields-occupied">—</span> von 300 Feldern belegt
</p>
```

Wird beim Laden via `GET /api/passivmitglieder/felder` → `occupiedCount` befüllt und nach
erfolgreicher Anmeldung im selben Request-Callback sofort inkrementiert.

### 8.4 Mobile-Optimierung

- SVG: `width="100%"`, `viewBox="0 0 840 440"`, vollständig responsive
- Formular-Inputs: `font-size: 16px` (verhindert iOS-Auto-Zoom)
- Modal auf Mobile: `position: fixed; inset: 0` (Vollbild)
- Stufenkarten: auf Mobile vertikal gestapelt
- Touch-Events: `touchstart` registriert für SVG-Felder

---

## 9. Umbraco Content Type & Template

### 9.1 uSync-Config `passivmitgliedschaft.config`

- Alias: `passivMitgliedschaft`
- Name: Passivmitgliedschaft
- Icon: `icon-favorite`
- Template: `PassivMitgliedschaft`
- Darf unter `homePage` angelegt werden (`homepage.config` → `<Structure>` erweitern)
- Eigenschaften: `pageHeading` (TextBox) – für den Header-Titel

### 9.2 Template `Views/PassivMitgliedschaft.cshtml`

```
1. Page-Header (kein Bild – dunkler Hintergrund mit Noise-Effekt, Barlow-Condensed-Headline)
2. Intro-Abschnitt: Text + Mitgliedsstufen-Cards
3. Live-Zähler
4. Interaktiver SVG-Bodenplan
5. Registrierungs-Modal (initial versteckt)
```

Lädt `/css/passivmitglied.css` und `/js/passivmitglied.js` via `@section Scripts`.
Lädt ausserdem das Cloudflare Turnstile Script:
`<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>`

Das Turnstile-Widget wird im letzten Wizard-Schritt als `<div class="cf-turnstile"
data-sitekey="...">` eingebettet. Beim Submit liest JS das Token aus dem versteckten
`cf-turnstile-response`-Input und schickt es als `captchaToken` im JSON-Body mit.

---

## 10. Admin-Bereich

### 10.1 Razor Page `/passivmitglieder-admin`

Zugriffsschutz: `[UmbracoAdminAuthorize]` (erst unter `/umbraco` einloggen).

**Mitgliederliste:**
- Tabellenansicht: Feld-Nr. | VIP | Stufe | Vorname | Nachname | E-Mail | Angemeldet | Bezahlt | Notizen
- Filter: Stufe, Bezahlstatus
- Sortierung: Feld-Nr., Stufe, Datum, Name

**Bezahlung markieren:**
Button/Checkbox → AJAX `POST /api/.../paid` → sofortiges visuelles Feedback (Haken, grüner Hintergrund)

**Notizen:**
Inline-`<textarea>` → AJAX-Save beim `blur`-Event

**Export:**
- "Als Excel exportieren" → `GET /api/.../admin/export/excel` (interne Verwaltung, inkl. Bezahlstatus)
- "Als AbaNinja CSV exportieren" → `GET /api/.../admin/export/abaninja` (direkt importierbar in AbaNinja für Rechnungsstellung)

---

## 11. Homepage-Teaser

Anpassung von `Views/Home.cshtml`: neuer Abschnitt nach dem Hero.

```html
<div class="passiv-teaser">
  <div class="passiv-teaser__inner">
    <p class="pre-label">Jetzt dabei sein</p>
    <h2>Passivmitgliedschaft</h2>
    <p>Werde Teil der Sporthalle Sulzerallee. Wähle deinen symbolischen
       Quadratmeter Hallenboden und unterstütze den Nachwuchssport in Winterthur.</p>
    <a href="/passivmitgliedschaft" class="btn-cta">Jetzt Mitglied werden</a>
  </div>
</div>
```

Design: Barlow-Condensed-Headline in Weiss auf Vereinsrot (`#EB504B`), Noise-Overlay wie
`.hero-announcement`, CTA-Button hell auf dunklem Hintergrund.

---

## 12. Implementierungsreihenfolge

1. **Domain-Schicht** (Aggregate Root, Value Objects, `VipField`, Ports)
2. **Infrastructure: Migration + Repository** (DB-Tabelle anlegen, CRUD via PetaPoco)
3. **Application Use Cases** (`RegisterMemberUseCase`, `GetFieldStatusesQuery`, `AdminService`)
4. **Brevo-Adapter** + Konfiguration; lokaler Test mit echtem Brevo-Dev-Account
5. **API-Controller** (alle Endpunkte)
6. **SVG-Bodenplan** (statische Anzeige belegter Felder + VIP-Felder + Live-Zähler)
7. **Multi-Step-Wizard** (Frontend) + API-Integration
8. **Passivmitgliedschaft-Template + uSync-Config**
9. **Admin-Bereich** (Razor Page + Excel-Export via ClosedXML)
10. **Homepage-Teaser**
11. **Mobile-Tests + Cross-Browser-Tests** (Chrome, Safari iOS, Android Chrome)
12. **Azure: `Brevo__ApiKey` setzen + End-to-End-Test**
13. **Umbraco Backoffice: Seite anlegen und publizieren**

---

## 13. Deployment-Checkliste

- [ ] `ClosedXML` NuGet zu `SporthalleWeb.csproj` hinzufügen
- [ ] Azure App Service Environment Variable `Brevo__ApiKey` setzen
- [ ] Azure App Service Environment Variables `Turnstile__SiteKey` und `Turnstile__SecretKey` setzen (aus dem Cloudflare Dashboard)
- [ ] `wwwroot/media/unihockey-boden.svg` ins Repository kopieren
- [ ] `appsettings.json`: `"Brevo"` und `"Turnstile"` Sektionen hinzufügen (Keys leer lassen, Werte nur via Env-Variable)
- [ ] DB-Migration läuft automatisch beim ersten App-Start
- [ ] uSync importiert ContentType + Template automatisch
- [ ] Umbraco Backoffice: Seite "Passivmitgliedschaft" anlegen, unter Homepage verschachteln, publizieren
- [ ] End-to-End: Feld wählen, alle Wizard-Schritte, Brevo-E-Mail prüfen, BCC an alle 3 Adressen verifizieren
- [ ] Admin-Bereich: Bezahlung markieren, Notiz speichern, Excel-Export prüfen
- [ ] AbaNinja-CSV-Export prüfen: Datei in AbaNinja unter "Kontakte > Importieren" hochladen und Felder-Mapping verifizieren
