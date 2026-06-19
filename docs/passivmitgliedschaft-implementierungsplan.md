# Implementierungsplan: Passivmitgliedschaft

Erstellt: 2026-06-17  
Blazor-Hybrid ergänzt: 2026-06-19

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
| Frontend | **Blazor Server** (Interactive Server Rendering) für alle interaktiven UI-Teile; Umbraco Razor für Layout-Shell |
| Sprache (Typen) | Alle C#-Typen (Klassen, Records, Enums, Interfaces, Methoden, Properties) werden auf **Englisch** benannt. Deutsch nur in UI-Texten, Fehlermeldungen und Kommentaren. |

### Hybridmodell: Umbraco + Blazor Server

Diese Lösung ist ein **Hybrid** — kein Plattformwechsel. Umbraco bleibt das CMS, Blazor
wird ausschliesslich für interaktive Komponenten-Inseln eingesetzt:

| Schicht | Technologie | Verantwortung |
|---------|-------------|---------------|
| CMS & Routing | Umbraco 17 | Seiten, Content-Types, Templates, URL-Routing |
| Layout & statische Inhalte | Razor-Templates (`.cshtml`) | Header, Footer, Umbraco-Felder, CSS-Einbindung |
| Interaktive Komponenten-Inseln | Blazor Server (`.razor`) | SVG-Grid, Anmeldewizard, Admin-Dashboard |
| Minimale JS-Brücke | Vanilla JS (stark reduziert) | Turnstile-Callback (JS-Interop-Stub) |
| Backend | ASP.NET Core Controller + Use Cases | API-Endpunkte, CSV/Excel-Export |

Blazor **ersetzt nicht** Umbraco oder das Razor-Templating-System. Blazor-Komponenten werden
via `<component type="typeof(...)" render-mode="Server" />` in bestehende Umbraco-Razor-Views
eingebettet. Alles ausserhalb der interaktiven Inseln bleibt klassisches Umbraco.

### Abgrenzung

- Keine direkte Zahlungsintegration. Die Jahresrechnung wird separat verschickt (ausserhalb dieser Anwendung).
- Admin-Bereich: per Umbraco-Login geschützte Razor Page, die eine Blazor-Komponente hostet.

---

## 2. Architektur

### 2.1 Hexagonale Architektur (Ports & Adapters)

Der Kern (Domain + Application) kennt keine Infrastrukturdetails. Die Blazor-Komponenten
sind **Inbound Adapters** — sie rufen Application-Layer Use Cases direkt auf (kein HTTP-Roundtrip
zur eigenen API nötig, da Blazor Server auf demselben Prozess läuft).

```
                     ┌──────────────────────────────────────────────────┐
  Browser ──────────►│  Presentation (Inbound Adapters)                  │
  (SignalR Circuit)  │  Blazor Server Components (InteractiveServer)     │
                     │  BodenplanComponent.razor                         │
                     │  RegistrierungsWizardComponent.razor              │
                     │  PassivMitgliederAdminComponent.razor             │
                     │                                                    │
                     │  Umbraco Razor Template (Shell)                   │
                     │  PassivMitgliedschaft.cshtml                      │
                     │  PassivMitgliederAdmin.cshtml (dünner Wrapper)    │
                     └──────────────────┬───────────────────────────────┘
                                        │ ruft auf (direkte DI-Injektion)
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
├── _Imports.razor                               Blazor-weite @using-Statements
│
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
    ├── Components/                             Blazor-Komponenten (NEU)
    │   ├── BodenplanComponent.razor            SVG-Grid + Live-Zähler
    │   ├── RegistrierungsWizardComponent.razor 6-Schritt-Anmeldung
    │   └── PassivMitgliederAdminComponent.razor Admin-Tabelle + Export
    ├── Dtos/
    │   ├── RegisterMemberRequest.cs            (bleibt für API-Fallback)
    │   └── FieldStatusResponse.cs
    └── Controllers/
        └── PassivMitgliederController.cs       (bleibt für externe API-Nutzung)

Views/
└── PassivMitgliedschaft.cshtml                 Umbraco Template (Shell mit <component>)
    Home.cshtml                                 Teaser-Abschnitt

Pages/
└── PassivMitgliederAdmin.cshtml               Dünner Razor-Page-Wrapper
    PassivMitgliederAdmin.cshtml.cs            [UmbracoAdminAuthorize] + leeres Page-Model

uSync/v17/
├── ContentTypes/
│   ├── passivmitgliedschaft.config
│   └── homepage.config
└── Templates/
    └── passivmitgliedschaft.config

wwwroot/
├── css/passivmitglied.css                     Verbleibt (Grid-Styles, Farben, Modal)
└── media/unihockey-boden.svg                  Statische SVG-Basis (ohne Grid-Overlay)
```

*`wwwroot/js/passivmitglied.js` entfällt — Interaktionslogik liegt in den Blazor-Komponenten.*

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
    public string AddressLine { get; private set; }
    public string PostalCode { get; private set; }
    public string City { get; private set; }
    public string Country { get; private set; }
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
    private static readonly HashSet<int> GoalCreaseLeft  = ComputeRange(cols: (1,3), rows: (5,9));
    private static readonly HashSet<int> GoalCreaseRight = ComputeRange(cols: (16,18), rows: (5,9));
    private static readonly HashSet<int> CenterCircle    = ComputeRange(cols: (8,11), rows: (5,9));
    private static readonly HashSet<int> FaceOffSpots    = new() { 2, 182, 181, 200, 20, 199 };

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
        await email.SendRegistrationConfirmationAsync(member);

        return member;
    }
}

public record RegisterMemberCommand(
    int FieldNumber, string FirstName, string LastName,
    string Address, string PostalCode, string City,
    string Email, string LevelKey,
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
    AddressLine     NVARCHAR(300) NOT NULL,
    PostalCode      NVARCHAR(20)  NOT NULL,
    City            NVARCHAR(100) NOT NULL,
    Country         NVARCHAR(100) NOT NULL DEFAULT 'Schweiz',
    Email           NVARCHAR(200) NOT NULL,
    MembershipLevel NVARCHAR(20)  NOT NULL,
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

### 6.3 AbaNinja-CSV-Adapter

Der Export erzeugt eine Semicolon-CSV-Datei im AbaNinja-Kontakte-Importformat.

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
                "", $"PM{m.Id:D4}", "", "", m.FirstName, m.LastName,
                m.Email.Value, "", "", "",
                m.AddressLine, "", "", "",
                m.PostalCode, m.City, m.Country,
                notes, "CHF", "Passivmitglied"
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
```

### 6.4 Excel-Adapter (ClosedXML)

NuGet: `ClosedXML`. Spalten: Nr. | Feld-Nr. | VIP-Label | Stufe | CHF/Jahr | Vorname | Nachname | Adresse | PLZ | Stadt | E-Mail | Angemeldet am | Bezahlt am | Notizen

### 6.5 CAPTCHA-Adapter (Cloudflare Turnstile)

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

Die Verifizierung erfolgt in `RegistrierungsWizardComponent.razor` via direktem Inject von
`ICaptchaPort`, bevor `RegisterMemberUseCase` aufgerufen wird:

```csharp
// In RegistrierungsWizardComponent.razor @code-Block:
[Inject] private ICaptchaPort Captcha { get; set; } = default!;

private async Task OnSubmitAsync()
{
    var remoteIp = ...; // via IHttpContextAccessor aus DI
    if (!await Captcha.VerifyAsync(CaptchaToken, remoteIp))
    {
        ErrorMessage = "CAPTCHA-Überprüfung fehlgeschlagen. Bitte versuche es erneut.";
        return;
    }
    // ... RegisterMemberUseCase.ExecuteAsync aufrufen
}
```

### 6.6 DI-Registrierung (`PassivMitgliederComposer`)

```csharp
public class PassivMitgliederComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<PassivMitgliederMigrationComponent>();

        builder.Services.Configure<BrevoEmailOptions>(builder.Config.GetSection("Brevo"));
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

Die REST-API bleibt bestehen (für externe Nutzung und als Fallback). Die Blazor-Komponenten
rufen die Application-Layer-Use-Cases jedoch **direkt** auf, ohne HTTP-Roundtrip.

```
GET  /api/passivmitglieder/felder
     → { occupiedFields: [{fieldNumber, displayName, vipLabel}], totalFields: 300, occupiedCount: N }

POST /api/passivmitglieder/register
     Body: RegisterMemberRequest
     → 200 OK | 409 Conflict | 400 Bad Request

POST /api/passivmitglieder/{id}/paid          [UmbracoAdminAuthorize]
POST /api/passivmitglieder/{id}/notes         [UmbracoAdminAuthorize]
GET  /api/passivmitglieder/admin/members      [UmbracoAdminAuthorize]
GET  /api/passivmitglieder/admin/export/excel     [UmbracoAdminAuthorize]
GET  /api/passivmitglieder/admin/export/abaninja  [UmbracoAdminAuthorize]
```

---

## 8. Frontend (Blazor Server Components)

### 8.1 Blazor-Setup in `Program.cs`

```csharp
// Nach builder.CreateUmbracoBuilder(...)
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor(); // für RemoteIp in Captcha

// Im App-Pipeline-Block, nach app.UseUmbraco():
app.MapBlazorHub();
```

In `Views/_ViewImports.cshtml` (bereits vorhanden oder anlegen):
```cshtml
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

### 8.2 `_Imports.razor`

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using SporthalleWeb.Application.PassivMitgliedschaft
@using SporthalleWeb.Domain.PassivMitgliedschaft
```

### 8.3 `BodenplanComponent.razor`

Ersetzt den kompletten SVG-Grid-Teil von `passivmitglied.js`. Die Komponente lädt den
Feldzustand direkt über `GetFieldStatusesQuery` (kein HTTP-Aufruf an die eigene API),
rendert das interaktive SVG-Grid und öffnet den Anmeldewizard bei Feldklick.

```razor
@rendermode InteractiveServer
@inject GetFieldStatusesQuery FieldQuery

<div class="bodenplan-container">

    <p class="field-counter">
        @if (IsLoading)
        {
            <span>…</span>
        }
        else
        {
            <span>@FieldData!.OccupiedFields.Count</span>
        }
        von 300 Feldern belegt
    </p>

    @if (IsLoading)
    {
        <div class="loading-spinner" role="status">Lade Bodenplan…</div>
    }
    else
    {
        <svg viewBox="0 0 840 440" width="100%"
             xmlns="http://www.w3.org/2000/svg"
             aria-label="Interaktiver Hallenbodenplan">

            <!-- Statische Hallen-Grafik via <use> aus unihockey-boden.svg -->
            <image href="/media/unihockey-boden.svg" x="0" y="0" width="840" height="440" />

            <!-- Interaktives Feld-Grid (300 Zellen) -->
            @for (var row = 0; row < Rows; row++)
            {
                @for (var col = 0; col < Cols; col++)
                {
                    var fieldNum = row * Cols + col + 1;
                    var field    = GetField(fieldNum);
                    var isVip    = VipField.IsVip(fieldNum);
                    var isFree   = field is null;
                    var cx       = FieldOffsetX + col * CellW;
                    var cy       = FieldOffsetY + row * CellH;

                    <rect x="@cx" y="@cy" width="@CellW" height="@CellH"
                          fill="@GetFill(field, isVip)"
                          stroke="@(isVip ? "#FFD700" : "transparent")"
                          stroke-width="@(isVip ? "1.5" : "0")"
                          class="grid-cell @(isFree ? "grid-cell--free" : "grid-cell--taken")"
                          role="@(isFree ? "button" : "img")"
                          aria-label="@GetAriaLabel(fieldNum, field, isVip)"
                          style="cursor: @(isFree ? "pointer" : "default")"
                          @onclick="() => OnFieldClick(fieldNum, isFree)" />

                    @if (field?.DisplayName is not null)
                    {
                        <text x="@(cx + CellW / 2.0)" y="@(cy + CellH / 2.0)"
                              text-anchor="middle" dominant-baseline="middle"
                              font-size="6" fill="white" pointer-events="none">
                            @TruncateName(field.DisplayName)
                        </text>
                    }
                }
            }
        </svg>
    }

    @if (SelectedField.HasValue)
    {
        <RegistrierungsWizardComponent
            FieldNumber="SelectedField.Value"
            OnClose="() => SelectedField = null"
            OnSuccess="OnRegistrationSuccess" />
    }

</div>

@code {
    private const int Rows = 15, Cols = 20;
    private const double FieldOffsetX = 20, FieldOffsetY = 20;
    private const double CellW = 40, CellH = 800.0 / 15;

    private bool IsLoading = true;
    private FieldStatusesResult? FieldData;
    private int? SelectedField;

    protected override async Task OnInitializedAsync()
    {
        FieldData = await FieldQuery.ExecuteAsync();
        IsLoading = false;
    }

    private FieldStatusDto? GetField(int num) =>
        FieldData?.OccupiedFields.FirstOrDefault(f => f.FieldNumber == num);

    private void OnFieldClick(int fieldNum, bool isFree)
    {
        if (isFree) SelectedField = fieldNum;
    }

    private async Task OnRegistrationSuccess(int newlyOccupiedField)
    {
        SelectedField = null;
        FieldData = await FieldQuery.ExecuteAsync(); // Grid sofort aktualisieren
    }

    private string GetFill(FieldStatusDto? field, bool isVip)
    {
        if (field is null)
            return isVip ? "rgba(255,215,0,0.15)" : "rgba(255,255,255,0.08)";
        return isVip
            ? "rgba(255,215,0,0.5)"
            : field.DisplayName is not null
                ? "rgba(235,80,75,0.9)"
                : "rgba(235,80,75,0.75)";
    }

    private string GetAriaLabel(int num, FieldStatusDto? field, bool isVip)
    {
        var label = isVip ? $" ({VipField.GetLabel(num)})" : "";
        if (field is null) return $"Feld {num}{label} – verfügbar";
        var name = field.DisplayName ?? "belegt (anonym)";
        return $"Feld {num}{label} – {name}";
    }

    private static string TruncateName(string name) =>
        name.Length > 5 ? name[..4] + "…" : name;
}
```

**Farbcodierung (identisch zum alten JS-Plan):**

| Zustand | Farbe |
|---------|-------|
| Verfügbar | `rgba(255,255,255,0.08)` |
| VIP (verfügbar) | `rgba(255,215,0,0.15)` + goldener Rahmen |
| VIP (belegt) | `rgba(255,215,0,0.5)` |
| Belegt, anonym | `rgba(235,80,75,0.75)` |
| Belegt, mit Name | `rgba(235,80,75,0.9)` |

### 8.4 `RegistrierungsWizardComponent.razor`

Ersetzt den 6-Schritt-JS-Wizard. Steuert den gesamten Anmelde-Flow inkl. Cloudflare Turnstile.

```razor
@rendermode InteractiveServer
@inject RegisterMemberUseCase RegisterUseCase
@inject ICaptchaPort Captcha
@inject IHttpContextAccessor HttpContextAccessor

<div class="modal-overlay" @onclick="OnClose">
    <div class="modal-content" @onclick:stopPropagation>

        <button class="modal-close" @onclick="OnClose" aria-label="Schliessen">×</button>
        <div class="step-indicator">Schritt @((int)CurrentStep) von 6</div>

        @switch (CurrentStep)
        {
            case WizardStep.FeldBestaetigen:
                <h2>Feld Nr. @FieldNumber bestätigen</h2>
                @if (VipField.IsVip(FieldNumber))
                {
                    <p class="badge-vip">@VipField.GetLabel(FieldNumber)</p>
                }
                <p class="hint">Der Hallenboden ist symbolisch – der physische Boden
                    verbleibt im Eigentum des Vereins.</p>
                <div class="wizard-actions">
                    <button class="btn-primary" @onclick="NextStep">Weiter</button>
                </div>
                break;

            case WizardStep.Stufe:
                <h2>Mitgliedsstufe wählen</h2>
                <div class="level-cards">
                    @foreach (var level in new[] { MembershipLevel.Bronze, MembershipLevel.Silber, MembershipLevel.Gold })
                    {
                        <div class="level-card @(SelectedLevelKey == level.Key ? "level-card--active" : "")"
                             @onclick="() => SelectedLevelKey = level.Key">
                            <strong>@level.DisplayName</strong>
                            <span class="price">CHF @level.YearlyFee.–/Jahr</span>
                        </div>
                    }
                </div>
                <div class="wizard-actions">
                    <button @onclick="PrevStep">Zurück</button>
                    <button class="btn-primary" disabled="@(SelectedLevelKey is null)"
                            @onclick="NextStep">Weiter</button>
                </div>
                break;

            case WizardStep.Daten:
                <h2>Persönliche Angaben</h2>
                <EditForm Model="this" OnValidSubmit="NextStep">
                    <DataAnnotationsValidator />
                    <div class="form-row">
                        <label>Vorname *<InputText @bind-Value="FirstName" /></label>
                        <label>Nachname *<InputText @bind-Value="LastName" /></label>
                    </div>
                    <label>Strasse + Nr. *<InputText @bind-Value="AddressLine" /></label>
                    <div class="form-row">
                        <label>PLZ *<InputText @bind-Value="PostalCode" /></label>
                        <label>Ort *<InputText @bind-Value="City" /></label>
                    </div>
                    <label>E-Mail *<InputText @bind-Value="Email" type="email" /></label>
                    <div class="wizard-actions">
                        <button type="button" @onclick="PrevStep">Zurück</button>
                        <button type="submit" class="btn-primary">Weiter</button>
                    </div>
                </EditForm>
                break;

            case WizardStep.NamensAnzeige:
                <h2>Namensanzeige auf dem Bodenplan</h2>
                <label>
                    <input type="radio" name="showname" checked="@(!ShowNameOnFloor)"
                           @onchange="() => ShowNameOnFloor = false" />
                    Anonym bleiben
                </label>
                <label>
                    <input type="radio" name="showname" checked="@ShowNameOnFloor"
                           @onchange="() => ShowNameOnFloor = true" />
                    Name anzeigen als: <InputText @bind-Value="DisplayName"
                                                  placeholder='z.B. "Max M." oder "Familie Meier"'
                                                  disabled="@(!ShowNameOnFloor)" />
                </label>
                <div class="wizard-actions">
                    <button @onclick="PrevStep">Zurück</button>
                    <button class="btn-primary" @onclick="NextStep">Weiter</button>
                </div>
                break;

            case WizardStep.Zusammenfassung:
                var level = MembershipLevel.FromKey(SelectedLevelKey!);
                <h2>Zusammenfassung</h2>
                <dl class="summary">
                    <dt>Feld-Nr.</dt><dd>@FieldNumber @(VipField.IsVip(FieldNumber) ? $"({VipField.GetLabel(FieldNumber)})" : "")</dd>
                    <dt>Stufe</dt><dd>@level.DisplayName – CHF @level.YearlyFee.–/Jahr</dd>
                    <dt>Name</dt><dd>@FirstName @LastName</dd>
                    <dt>Adresse</dt><dd>@AddressLine, @PostalCode @City (Schweiz)</dd>
                    <dt>E-Mail</dt><dd>@Email</dd>
                    <dt>Anzeige</dt><dd>@(ShowNameOnFloor ? $'"{DisplayName}"' : "Anonym")</dd>
                </dl>
                <p class="legal-notice">
                    Der Hallenboden ist SYMBOLISCH. "Besitzer:in eines Quadratmeters" ist eine
                    Metapher für die Mitgliedschaft. Der physische Boden verbleibt im Eigentum des Vereins.
                </p>
                <label class="checkbox-label">
                    <input type="checkbox" @bind="Consent" />
                    Ich möchte Passivmitglied werden und erkenne die jährliche Beitragspflicht
                    (bis auf Widerruf) an.*
                </label>

                <!-- Cloudflare Turnstile Widget -->
                <div class="cf-turnstile" data-sitekey="@TurnstileSiteKey"
                     data-callback="onTurnstileSuccess"></div>

                @if (ErrorMessage is not null)
                {
                    <p class="error-message" role="alert">@ErrorMessage</p>
                }
                <div class="wizard-actions">
                    <button @onclick="PrevStep" disabled="@IsSubmitting">Zurück</button>
                    <button class="btn-primary"
                            disabled="@(!Consent || string.IsNullOrEmpty(CaptchaToken) || IsSubmitting)"
                            @onclick="OnSubmitAsync">
                        @(IsSubmitting ? "Wird angemeldet…" : "Jetzt anmelden")
                    </button>
                </div>
                break;

            case WizardStep.Bestaetigung:
                <div class="success-banner">
                    <h2>Vielen Dank, @FirstName!</h2>
                    <p>Du erhältst in Kürze eine Bestätigung an @Email.</p>
                    <p>Die Rechnung für den Jahresbeitrag wird separat zugestellt.</p>
                    <button @onclick="OnCloseAfterSuccess">Schliessen</button>
                </div>
                break;
        }
    </div>
</div>

@code {
    private enum WizardStep
    {
        FeldBestaetigen = 1, Stufe, Daten, NamensAnzeige, Zusammenfassung, Bestaetigung
    }

    [Parameter] public int FieldNumber { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<int> OnSuccess { get; set; }

    [Inject] private IConfiguration Config { get; set; } = default!;

    private string TurnstileSiteKey => Config["Turnstile:SiteKey"] ?? "";

    private WizardStep CurrentStep = WizardStep.FeldBestaetigen;
    private string? SelectedLevelKey;

    [Required] private string FirstName = "";
    [Required] private string LastName = "";
    [Required] private string AddressLine = "";
    [Required] private string PostalCode = "";
    [Required] private string City = "";
    [Required, EmailAddress] private string Email = "";
    private bool ShowNameOnFloor;
    private string? DisplayName;
    private bool Consent;
    private string? CaptchaToken;
    private bool IsSubmitting;
    private string? ErrorMessage;

    private void NextStep() => CurrentStep++;
    private void PrevStep() => CurrentStep--;

    // Turnstile-Callback: JS ruft diese Methode via DotNetObjectReference auf
    [JSInvokable]
    public void SetCaptchaToken(string token) { CaptchaToken = token; StateHasChanged(); }

    private async Task OnSubmitAsync()
    {
        IsSubmitting = true;
        ErrorMessage = null;
        try
        {
            var remoteIp = HttpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "";
            if (!await Captcha.VerifyAsync(CaptchaToken!, remoteIp))
            {
                ErrorMessage = "CAPTCHA-Überprüfung fehlgeschlagen.";
                return;
            }

            var cmd = new RegisterMemberCommand(
                FieldNumber, FirstName, LastName,
                AddressLine, PostalCode, City,
                Email, SelectedLevelKey!,
                ShowNameOnFloor, DisplayName, Consent);

            await RegisterUseCase.ExecuteAsync(cmd);
            CurrentStep = WizardStep.Bestaetigung;
        }
        catch (FieldAlreadyTakenException)
        {
            ErrorMessage = "Dieses Feld wurde soeben von jemand anderem belegt. Bitte wähle ein anderes.";
        }
        catch (DomainException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task OnCloseAfterSuccess()
    {
        await OnSuccess.InvokeAsync(FieldNumber);
    }
}
```

**Hinweis Turnstile:** Das Cloudflare-Widget setzt `CaptchaToken` via `data-callback`. In `wwwroot/js/passivmitglied.js` (verbleibt als kleiner Stub) wird die globale Callback-Funktion registriert:

```js
// wwwroot/js/passivmitglied.js (stark reduziert)
window.onTurnstileSuccess = (token) => {
  if (window._blazorWizardRef) {
    window._blazorWizardRef.invokeMethodAsync('SetCaptchaToken', token);
  }
};
```

Der `_blazorWizardRef` wird in `RegistrierungsWizardComponent` via `IJSRuntime.InvokeVoidAsync("registerBlazorRef", DotNetObjectReference.Create(this))` gesetzt.

### 8.5 `PassivMitgliederAdminComponent.razor`

Ersetzt `PassivMitgliederAdmin.cshtml` + alle AJAX-Snippets. Die Razor Page wird zum
dünnen Wrapper (siehe Abschnitt 9.2).

```razor
@rendermode InteractiveServer
@inject AdminService AdminSvc
@inject IPassivMitgliederRepository Repo
@inject NavigationManager Nav

<div class="admin-panel">
    <h1>Passivmitglieder-Verwaltung</h1>

    <div class="admin-toolbar">
        <a href="/api/passivmitglieder/admin/export/excel" class="btn-secondary">
            Als Excel exportieren
        </a>
        <a href="/api/passivmitglieder/admin/export/abaninja" class="btn-secondary">
            Als AbaNinja CSV exportieren
        </a>
    </div>

    @if (IsLoading)
    {
        <p>Lade Mitglieder…</p>
    }
    else
    {
        <table class="admin-table">
            <thead>
                <tr>
                    <th @onclick='() => SortBy("FieldNumber")'>Feld-Nr.</th>
                    <th>VIP</th>
                    <th @onclick='() => SortBy("Level")'>Stufe</th>
                    <th @onclick='() => SortBy("LastName")'>Name</th>
                    <th>E-Mail</th>
                    <th @onclick='() => SortBy("CreatedAt")'>Angemeldet</th>
                    <th>Bezahlt</th>
                    <th>Notizen</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var m in SortedMembers)
                {
                    <tr class="@(m.PaidAt.HasValue ? "row-paid" : "")">
                        <td>@m.FieldNumber.Value</td>
                        <td>@(VipField.IsVip(m.FieldNumber.Value) ? VipField.GetLabel(m.FieldNumber.Value) : "")</td>
                        <td>@m.Level.DisplayName</td>
                        <td>@m.FirstName @m.LastName</td>
                        <td>@m.Email.Value</td>
                        <td>@m.CreatedAt.ToString("dd.MM.yyyy")</td>
                        <td>
                            @if (m.PaidAt.HasValue)
                            {
                                <span class="paid-checkmark" title="@m.PaidAt.Value.ToString("dd.MM.yyyy")">✓</span>
                            }
                            else
                            {
                                <button class="btn-small" @onclick="() => MarkAsPaidAsync(m.Id)">
                                    Als bezahlt markieren
                                </button>
                            }
                        </td>
                        <td>
                            <textarea rows="1"
                                      @bind="NotesBuffer[m.Id]"
                                      @onblur="() => SaveNotesAsync(m.Id)" />
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
</div>

@code {
    private bool IsLoading = true;
    private IReadOnlyList<PassivMitglied> Members = [];
    private string SortColumn = "FieldNumber";
    private Dictionary<int, string?> NotesBuffer = [];

    private IEnumerable<PassivMitglied> SortedMembers => SortColumn switch
    {
        "Level"     => Members.OrderBy(m => m.Level.YearlyFee),
        "LastName"  => Members.OrderBy(m => m.LastName),
        "CreatedAt" => Members.OrderByDescending(m => m.CreatedAt),
        _           => Members.OrderBy(m => m.FieldNumber.Value)
    };

    protected override async Task OnInitializedAsync()
    {
        Members = await Repo.GetAllAsync();
        NotesBuffer = Members.ToDictionary(m => m.Id, m => m.Notes);
        IsLoading = false;
    }

    private void SortBy(string column) => SortColumn = column;

    private async Task MarkAsPaidAsync(int memberId)
    {
        await AdminSvc.MarkAsPaidAsync(memberId);
        Members = await Repo.GetAllAsync();
    }

    private async Task SaveNotesAsync(int memberId)
    {
        if (NotesBuffer.TryGetValue(memberId, out var notes))
            await AdminSvc.UpdateNotesAsync(memberId, notes);
    }
}
```

---

## 9. Umbraco Content Type & Template

### 9.1 uSync-Config `passivmitgliedschaft.config`

- Alias: `passivMitgliedschaft`
- Name: Passivmitgliedschaft
- Icon: `icon-favorite`
- Template: `PassivMitgliedschaft`
- Darf unter `homePage` angelegt werden
- Eigenschaften: `pageHeading` (TextBox)

### 9.2 Template `Views/PassivMitgliedschaft.cshtml`

Das Template ist jetzt eine **Shell**: Layout, Header, statischer Intro-Text aus Umbraco.
Die interaktiven Teile werden als Blazor-Komponenten eingebettet.

```cshtml
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = "Layout.cshtml";
}

<section class="page-header page-header--dark">
    <h1>@Model.Value("pageHeading")</h1>
</section>

<div class="content-narrow">
    <p>Werde Teil der Sporthalle Sulzerallee …</p>
    <!-- Mitgliedsstufen-Info-Cards (statischer HTML-Block) -->
</div>

<!-- Blazor-Komponente: SVG-Bodenplan + Live-Zähler + Wizard -->
<component type="typeof(BodenplanComponent)" render-mode="Server" />

<!-- Blazor Hub Script (einmalig im Layout oder hier) -->
<script src="/_blazor"></script>
<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<script src="/js/passivmitglied.js"></script>
```

### 9.3 Admin-Wrapper `Pages/PassivMitgliederAdmin.cshtml`

```cshtml
@page
@model PassivMitgliederAdminModel
@{
    Layout = null;
}
<!DOCTYPE html>
<html>
<head><title>Passivmitglieder Admin</title></head>
<body>
    <component type="typeof(PassivMitgliederAdminComponent)" render-mode="Server" />
    <script src="/_blazor"></script>
</body>
</html>
```

```csharp
// PassivMitgliederAdmin.cshtml.cs
[UmbracoAdminAuthorize]
public class PassivMitgliederAdminModel : PageModel
{
    public void OnGet() { }
}
```

---

## 10. Homepage-Teaser

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

---

## 11. Mobile-Optimierung

- SVG: `width="100%"`, `viewBox="0 0 840 440"`, vollständig responsive
- Formular-Inputs: `font-size: 16px` in `passivmitglied.css` (verhindert iOS-Auto-Zoom)
- Modal auf Mobile: `.modal-overlay { position: fixed; inset: 0 }` (Vollbild)
- Stufenkarten: auf Mobile via CSS Media Query vertikal gestapelt
- Touch-Events: Blazor-`@onclick` funktioniert auf Touch-Geräten nativ

---

## 12. Implementierungsreihenfolge

1. **Domain-Schicht** (Aggregate Root, Value Objects, `VipField`, Ports)
2. **Infrastructure: Migration + Repository** (DB-Tabelle, PetaPoco-CRUD)
3. **Application Use Cases** (`RegisterMemberUseCase`, `GetFieldStatusesQuery`, `AdminService`)
4. **Brevo-Adapter + Captcha-Adapter** (Konfiguration, lokaler Test)
5. **REST-Controller** (für API-Endpunkte + Export-Downloads)
6. **Blazor-Setup** (`Program.cs`: `AddServerSideBlazor()`, `MapBlazorHub()`, `_Imports.razor`)
7. **`BodenplanComponent.razor`** (SVG-Grid, Feldstatus, Live-Zähler, VIP-Highlighting)
8. **`RegistrierungsWizardComponent.razor`** (6 Schritte, Formularvalidierung, Turnstile, Use-Case-Integration)
9. **Umbraco-Template-Shell** (`PassivMitgliedschaft.cshtml` mit `<component>`)
10. **`PassivMitgliederAdminComponent.razor`** (Tabelle, Inline-Bezahlung, Notizen, Export-Links)
11. **Admin-Wrapper-Page** (`PassivMitgliederAdmin.cshtml` + `[UmbracoAdminAuthorize]`)
12. **Homepage-Teaser**
13. **Mobile-Tests** (Chrome, Safari iOS, Android Chrome)
14. **Azure: `Brevo__ApiKey`, `Turnstile__SiteKey`, `Turnstile__SecretKey` setzen + End-to-End-Test**
15. **Umbraco Backoffice: Seite anlegen und publizieren**

---

## 13. Deployment-Checkliste

- [ ] `ClosedXML` NuGet zu `SporthalleWeb.csproj` hinzufügen
- [ ] `Program.cs`: `AddServerSideBlazor()` und `MapBlazorHub()` eintragen
- [ ] `_Imports.razor` anlegen (Blazor-weite Usings)
- [ ] Azure App Service Environment Variable `Brevo__ApiKey` setzen
- [ ] Azure App Service Environment Variables `Turnstile__SiteKey` und `Turnstile__SecretKey` setzen
- [ ] `wwwroot/media/unihockey-boden.svg` ins Repository
- [ ] `appsettings.json`: `"Brevo"` und `"Turnstile"` Sektionen hinzufügen (Keys leer)
- [ ] DB-Migration läuft automatisch beim ersten App-Start
- [ ] uSync importiert ContentType + Template automatisch
- [ ] Umbraco Backoffice: Seite "Passivmitgliedschaft" anlegen, unter Homepage verschachteln, publizieren
- [ ] End-to-End: Feld wählen, alle 6 Wizard-Schritte, Turnstile-Widget erscheint in Schritt 5, Brevo-E-Mail prüfen, BCC an alle 3 Adressen verifizieren
- [ ] Blazor SignalR-Verbindung in Browser-DevTools prüfen (Network → WS → `/_blazor`)
- [ ] Admin-Bereich: Bezahlung markieren, Notiz speichern, Excel-Export und AbaNinja-CSV prüfen
- [ ] AbaNinja-CSV in AbaNinja unter "Kontakte > Importieren" hochladen und Felder-Mapping verifizieren
- [ ] Mobile-Test: SVG responsiv, Modal Vollbild auf iOS Safari
