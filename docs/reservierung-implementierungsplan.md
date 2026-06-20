# Implementierungsplan: Hallenbelegungs- und Reservierungstool

Erstellt: 2026-06-17  
Blazor-Hybrid ergänzt: 2026-06-19

---

## 1. Übersicht & Ziele

Mieter (Vereine, Firmen, Privatpersonen) buchen Zeitblöcke in der Sporthalle über einen
Wochenkalender. Die Buchung läuft wahlweise via Magic Link oder Passwort. Der Hallenverwalter bestätigt
oder lehnt ab. Serientermine legt ausschliesslich der Verwalter an; daraus generiert das
System Einzelbuchungen, die er bei Bedarf einzeln löschen kann (Ferienausnahmen etc.).
Für die Buchhaltung gibt es einen CSV-Export aller Einzelbuchungen mit Preisen.

### Getroffene Entscheidungen

| Thema | Entscheid |
|-------|-----------|
| Authentifizierung | **Hybrid:** Magic Link (immer möglich) + optionales Passwort (BCrypt, min. 8 Zeichen). Magic Link gültig 20 Min., einmalig einlösbar. |
| Passwort optional | Bei Registrierung wählbar; kann jederzeit nach Magic-Link-Login gesetzt oder geändert werden; ohne Passwort bleibt Magic-Link die einzige Login-Methode |
| Rate-Limit | Magic Link + Passwort-Reset: max. 1 E-Mail pro 10 Minuten pro Adresse |
| E-Mail-Sparsamkeit | Registrierungsbestätigung enthält bereits den ersten Magic Link — kein separates Login-Mail nötig |
| Datenbank-Zugriff | PetaPoco (konsistent mit Passivmitgliedschaft) |
| E-Mail-Provider | Brevo, Template ID 1, API-Key als `BREVO_API_KEY` Env-Variable |
| Admin-BCC | Buchungsbestätigungen: BCC an `jan.haug@sporthalle-sulzerallee.ch` |
| Kalender-Zeitzone | Europe/Zurich; DB-Speicherung UTC |
| Kalenderansicht | Belegungsübersicht: schreibgeschützte Wochenansicht (Desktop) / Tagansicht (Mobile) |
| Buchungs-Picker | Interaktiver 4-Schritt-Picker: Dauer wählen → Datum wählen (Monatskalender) → Zeit wählen (Slot-Liste) → Bestätigen + Anlass; implementiert als Blazor-Komponente |
| Buchbare Dauern | Konfigurierbares Menü aus Vielfachen des Blocks (z.B. 30 / 60 / 90 / 120 Min.), in Umbraco pflegbar |
| Anlass (Buchungstyp) | Dropdown aus konfigurierbarer Liste (Umbraco), z.B. Training, Turnier, Schulung, Privat, Sonstiges |
| Blockgrösse | 30 Min. (konfigurierbar in Umbraco) |
| Mindestbuchung | 1 Block (30 Min.) |
| Preismodell | Fix-Preis pro Block aus Umbraco-Konfig, manuell anpassbar; Serienbuchungen ohne Preis |
| CAPTCHA | Cloudflare Turnstile (wie Passivmitgliedschaft) |
| Serienbuchungen | Generator-Modell: RecurringRule → Einzelbuchungen in DB |
| Serienfarben | Admin wählt pro Serienregel eine Farbe (Hex); wird auf alle generierten Slots vererbt und im Kalender angezeigt |
| Schulferien-Integration | `SchoolHolidays`-Tabelle; `ExcludeSchoolHolidays`-Flag an `RecurringRules`; Generator überspringt Feriendaten automatisch |
| Mobile-First | Tagansicht (ein Tag) auf Mobilgeräten, Wochenansicht auf Desktop |
| Audit | Append-Only-Tabelle `BookingAuditLog` für alle Zustandsänderungen |
| Frontend | **Blazor Server** (Interactive Server Rendering) für alle interaktiven UI-Teile; Umbraco Razor für Layout-Shell |
| Member-Store | **Umbraco Members** (`IMemberManager` / `SignInManager`). BCrypt, Password-Reset-Tokens und Account-Lockout via ASP.NET Core Identity. Admin-Sicht im Umbraco Backoffice unter "Members". Keine eigene `HallRenters`-Tabelle. |
| Sessions | ASP.NET Core Identity Cookie (Umbraco Member-Cookie); keine eigene `HallSessions`-Tabelle nötig |
| Sprache (Typen) | Alle C#-Typen (Klassen, Records, Enums, Interfaces, Methoden, Properties) werden auf **Englisch** benannt. Deutsch nur in UI-Texten, Fehlermeldungen und Kommentaren. |

### Hybridmodell: Umbraco + Blazor Server

Diese Lösung ist ein **Hybrid** — kein Plattformwechsel. Umbraco bleibt das CMS, Blazor
wird ausschliesslich für interaktive Komponenten-Inseln eingesetzt:

| Schicht | Technologie | Verantwortung |
|---------|-------------|---------------|
| CMS & Routing | Umbraco 17 | Seiten, Content-Types, Templates, URL-Routing |
| Layout & statische Inhalte | Razor-Templates (`.cshtml`) | Header, Footer, Umbraco-Felder, CSS-Einbindung |
| Interaktive Komponenten-Inseln | Blazor Server (`.razor`) | Wochenkalender, Buchungs-Picker, Admin-Dashboard |
| Minimale JS-Brücke | Vanilla JS (stark reduziert) | Swipe-Events, Turnstile-Callback (JS-Interop-Stubs) |
| Backend | ASP.NET Core Controller + Use Cases | API-Endpunkte, Magic-Link-Auth, CSV-Export |

Blazor **ersetzt nicht** Umbraco oder das Razor-Templating-System. Blazor-Komponenten werden
via `<component type="typeof(...)" render-mode="Server" />` in bestehende Umbraco-Razor-Views
eingebettet. Alles ausserhalb der interaktiven Inseln bleibt klassisches Umbraco.

---

## 2. Architektur

### 2.1 Hexagonale Architektur (Ports & Adapters)

Dieselbe Struktur wie `PassivMitgliedschaft`: Domain-Kern kennt keine Infrastruktur.

```
  Browser ──────► Presentation (Inbound Adapters)
  (SignalR)        Blazor Server Components (InteractiveServer)
                   WochenkalenderComponent.razor
                   BuchungsPickerComponent.razor
                   ReservierungAdminComponent.razor
                   │
                   Umbraco Razor Template (Shell)
                   Reservierung.cshtml
                   ReservierungAdmin.cshtml (dünner Wrapper)
                   │
                   MVC Controller (Auth-Flow, Magic Link Redirect)
                   ReservierungController.cs
                        │
                   Application Layer (Use Cases)
                   SendMagicLinkUseCase
                   ValidateMagicLinkUseCase
                   CreateBookingUseCase
                   ConfirmBookingUseCase / RejectBookingUseCase
                   CreateRecurringRuleUseCase
                   GetWeekSlotsQuery / BookingAdminService
                        │ nutzt Ports
                   Domain (Kern)
                   HallMember (record, wraps IMember)
                   BookingSlot (Aggregate Root)
                   RecurringRule (Aggregate Root)
                   TimeSlot, RenterEmail, RenterType,
                   BookingStatus (Value Objects)
                   IMemberManagerPort
                   IBookingSlotRepository
                   IRecurringRuleRepository
                   IMagicLinkTokenRepository
                   IBookingEmailPort
                   IBookingCsvPort
                        │ implementiert durch
                   Infrastructure (Outbound Adapters)
                   UmbracoMemberAdapter (IMemberManager + SignInManager)
                   BookingSlotRepository (PetaPoco)
                   RecurringRuleRepository (PetaPoco)
                   MagicLinkTokenRepository (PetaPoco)
                   BrevoBookingEmailAdapter (HttpClient)
                   BookingCsvAdapter
                   ReservierungMigration
```

### 2.2 DDD (pragmatisch)

- **Aggregate Roots:** `BookingSlot`, `RecurringRule` kapseln ihre Zustandsübergänge. `HallRenter` entfällt: Mieterprofil und Auth liegen in Umbraco Members (ASP.NET Core Identity).
- **Value Objects:** `TimeSlot`, `RenterEmail`, `RenterType`, `BookingStatus` mit eingebetteter Validierungslogik.
- **Domain Events:** Einfache Records ohne Event-Bus. Verwendung nur für Audit-Einträge.
- **Repository Ports:** Im Domain-Kern definiert, PetaPoco-Adapter in Infrastructure. Ausnahme: `IMemberManagerPort` wraps `IMemberManager` (Umbraco Identity) für Testbarkeit.

---

## 3. Dateistruktur

```
src/SporthalleWeb/
├── Domain/Reservierung/
│   ├── HallMember.cs                        Record, wraps Umbraco IMember-Daten
│   ├── BookingSlot.cs
│   ├── RecurringRule.cs
│   ├── MagicLinkToken.cs
│   ├── TimeSlot.cs                          Value Object
│   ├── RenterEmail.cs                       Value Object
│   ├── RenterType.cs                        Value Object / Enum-Wrapper
│   ├── BookingStatus.cs                     Value Object / Enum-Wrapper
│   ├── DomainException.cs
│   └── Ports/
│       ├── IMemberManagerPort.cs            Wraps IMemberManager + SignInManager
│       ├── IBookingSlotRepository.cs
│       ├── IRecurringRuleRepository.cs
│       ├── IMagicLinkTokenRepository.cs
│       ├── IBookingAuditRepository.cs
│       ├── IBookingEmailPort.cs
│       └── IBookingCsvPort.cs
│
├── Application/Reservierung/
│   ├── SendMagicLinkUseCase.cs
│   ├── ValidateMagicLinkUseCase.cs
│   ├── RegisterRenterUseCase.cs
│   ├── RegisterRenterCommand.cs
│   ├── LoginWithPasswordUseCase.cs
│   ├── SetPasswordUseCase.cs
│   ├── RequestPasswordResetUseCase.cs
│   ├── ResetPasswordUseCase.cs
│   ├── CreateBookingUseCase.cs
│   ├── CreateBookingCommand.cs
│   ├── ConfirmBookingUseCase.cs
│   ├── RejectBookingUseCase.cs
│   ├── CreateRecurringRuleUseCase.cs
│   ├── CreateRecurringRuleCommand.cs
│   ├── GetWeekSlotsQuery.cs
│   ├── WeekSlotDto.cs
│   ├── GetAvailableDaysQuery.cs
│   ├── GetAvailableTimeSlotsQuery.cs
│   ├── SlotOption.cs
│   └── BookingAdminService.cs
│
├── Infrastructure/Reservierung/
│   ├── Members/
│   │   └── UmbracoMemberAdapter.cs          Implementiert IMemberManagerPort via IMemberManager + SignInManager
│   ├── Persistence/
│   │   ├── BookingSlotRepository.cs
│   │   ├── RecurringRuleRepository.cs
│   │   ├── MagicLinkTokenRepository.cs
│   │   ├── BookingAuditRepository.cs
│   │   ├── DbRecords/                       PetaPoco POCO-Klassen
│   │   │   ├── BookingSlotRecord.cs
│   │   │   ├── RecurringRuleRecord.cs
│   │   │   ├── MagicLinkTokenRecord.cs
│   │   │   ├── BookingAuditLogRecord.cs
│   │   │   └── SchoolHolidayRecord.cs
│   │   ├── SchoolHolidayRepository.cs
│   │   └── ReservierungMigration.cs         Erstellt 4 Tabellen (kein HallRenters/HallSessions/PasswordResetTokens)
│   ├── Email/
│   │   └── BrevoBookingEmailAdapter.cs
│   ├── Export/
│   │   └── BookingCsvAdapter.cs
│   └── Composition/
│       └── ReservierungComposer.cs

uSync/v17/
├── ContentTypes/
│   ├── reservierung.config
│   └── reservierungKonfiguration.config
├── MemberTypes/
│   └── hallMember.config                    Umbraco Member Type mit Custom-Properties
└── Templates/
    └── reservierung.config
│
├── _Imports.razor                               Blazor-weite @using-Statements
│
└── Presentation/Reservierung/
    ├── Components/                             Blazor-Komponenten (NEU)
    │   ├── WochenkalenderComponent.razor       Wochenansicht + Tagansicht (Mobile)
    │   ├── BuchungsPickerComponent.razor       4-Schritt-Buchungs-Flow
    │   └── ReservierungAdminComponent.razor    Admin-Dashboard (Buchungen, Serien, Ferien)
    ├── Controllers/
    │   ├── ReservierungController.cs           Auth: Magic-Link validate → Cookie setzen
    │   └── ReservierungAdminController.cs      CSV-Export-Download
    ├── Pages/
    │   ├── ReservierungAdmin.cshtml            Dünner Wrapper für Blazor-Admin-Komponente
    │   └── ReservierungAdmin.cshtml.cs         [UmbracoAdminAuthorize]
    └── Dtos/
        ├── WeekSlotResponse.cs
        ├── CreateBookingRequest.cs
        ├── SendMagicLinkRequest.cs
        ├── ValidateMagicLinkRequest.cs
        ├── RegisterRenterRequest.cs
        └── BookingAdminResponse.cs

Views/
└── Reservierung.cshtml                          Umbraco Template (Shell mit <component>)
    └── ReservierungKonfiguration.cshtml         (Konfig-Knoten, nicht öffentlich)

wwwroot/
└── css/reservierung.css
```

---

## Phase 1: Datenmodell & Datenbank

### 1.1 Datenbankschema

Die vier eigenen Tabellen (Buchungen, Serien, Audit, Magic-Link-Tokens) werden via
`IMigrationPlan` + `IComposer` beim App-Start angelegt. Mieterprofil und Auth liegen
vollständig in Umbraco Members — keine eigenen `HallRenters`-, `HallSessions`- oder
`PasswordResetTokens`-Tabellen.

#### Umbraco Member Type `hallMember` — Mieterprofil

Statt einer eigenen DB-Tabelle wird ein Umbraco Member Type mit dem Alias `hallMember`
angelegt (via `ReservierungComposer` oder uSync). Jeder Mieter ist ein Umbraco Member.

**Custom Properties des Member Types:**

| Alias | Typ | Pflicht | Bedeutung |
|-------|-----|---------|-----------|
| `renterType` | TextBox | Ja | `'Verein'` \| `'Firma'` \| `'Privatperson'` |
| `billingName` | TextBox | Ja | Firma oder vollständiger Name |
| `billingAddress` | TextBox | Ja | Strasse + Hausnummer |
| `billingPostalCode` | TextBox | Ja | PLZ |
| `billingCity` | TextBox | Ja | Ort |
| `billingCountry` | TextBox | Ja | Default: `'Schweiz'` |
| `phone` | TextBox | Nein | Telefonnummer |
| `hasKey` | True/False | Ja | Hat der Mieter einen Hallenschlüssel? |
| `magicLinkSentAt` | DateTime | Nein | Rate-Limit: Zeitstempel des letzten Magic Links |
| `passwordResetSentAt` | DateTime | Nein | Rate-Limit: Zeitstempel des letzten Passwort-Resets |

Standard-Identity-Felder via `IMemberManager` / `MemberIdentityUser`:
- `Email` (eindeutig, Username = E-Mail)
- `Name` (Ansprechperson)
- `PasswordHash` (BCrypt via Identity)
- `IsApproved`, `IsLockedOut`, `AccessFailedCount` (Lockout nach N Fehlversuchen, konfigurierbar)

**Anlegen via Composer:**
```csharp
// In ReservierungComposer.Run():
var memberTypeService = context.ServiceProvider.GetRequiredService<IMemberTypeService>();
if (memberTypeService.Get("hallMember") is null)
{
    var mt = new MemberType(shortStringHelper, -1)
    {
        Alias = "hallMember", Name = "Hall Renter"
    };
    // Properties hinzufügen via mt.AddPropertyType(...)
    memberTypeService.Save(mt);
}
```

#### `MagicLinkTokens` — Passwordless-Auth-Tokens

```sql
CREATE TABLE MagicLinkTokens (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    MemberId        INT           NOT NULL,         -- Umbraco IMember.Id
    TokenHash       NVARCHAR(128) NOT NULL UNIQUE,  -- SHA-256 des Klartexttokens
    ExpiresAt       DATETIME2     NOT NULL,
    UsedAt          DATETIME2     NULL,
    CreatedAt       DATETIME2     NOT NULL,
    RemoteIp        NVARCHAR(45)  NULL
)
```

Das Klartext-Token (64 zufällige Bytes, Base64url-codiert) wird nur per E-Mail verschickt
und nie in der DB gespeichert. In der DB liegt ausschliesslich der SHA-256-Hash.
Passwort-Reset-Tokens: werden von `IMemberManager.GeneratePasswordResetTokenAsync()`
(ASP.NET Core Identity, TOTP-basiert, zeitbegrenzt) erzeugt — keine eigene Tabelle.

#### `BookingSlots` — Einzelbuchungen

```sql
CREATE TABLE BookingSlots (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    MemberId            INT           NULL,   -- Umbraco IMember.Id; NULL bei Serienbuchungen
    RecurringRuleId     INT           NULL REFERENCES RecurringRules(Id),
    Status              NVARCHAR(20)  NOT NULL DEFAULT 'Provisorisch',
                                      -- 'Provisorisch' | 'Bestätigt' | 'Storniert'
    StartUtc            DATETIME2     NOT NULL,
    EndUtc              DATETIME2     NOT NULL,
    PricePerBlock       DECIMAL(10,2) NULL,   -- NULL bei Serienbuchungen
    TotalBlocks         INT           NULL,   -- Anzahl 30-Min-Blöcke
    TotalPrice          DECIMAL(10,2) NULL,   -- NULL bei Serienbuchungen
    PriceNote           NVARCHAR(500) NULL,   -- Freitext für manuelle Preisanpassung
    IsRecurringSlot     BIT           NOT NULL DEFAULT 0,
    Color               NVARCHAR(7)   NULL,   -- Vererbt von RecurringRule.Color; bei Einzelbuchungen NULL
    EventType           NVARCHAR(100) NULL,   -- Anlass, z.B. "Training", "Turnier"; aus konfigurierbarer Liste
    Notes               NVARCHAR(MAX) NULL,
    CreatedAt           DATETIME2     NOT NULL,
    UpdatedAt           DATETIME2     NOT NULL,
    CreatedBy           NVARCHAR(200) NOT NULL,  -- E-Mail des Nutzers oder 'admin'

    CONSTRAINT CK_BookingSlots_EndAfterStart CHECK (EndUtc > StartUtc)
)

-- Partieller Index: verhindert Überlappungen für aktive Buchungen auf DB-Ebene
-- (SQL Server unterstützt keinen nativen Overlap-Constraint; Konfliktprüfung daher im
--  Application Layer + Serialisierung via SELECT ... WITH (UPDLOCK, HOLDLOCK) in Transaction)
CREATE INDEX IX_BookingSlots_Time ON BookingSlots (StartUtc, EndUtc) WHERE Status <> 'Storniert'
```

#### `RecurringRules` — Serienregeln

```sql
CREATE TABLE RecurringRules (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    MemberId        INT           NULL,   -- Umbraco IMember.Id
    Description     NVARCHAR(300) NOT NULL,   -- z.B. "Hockeytraining Donnerstag"
    DayOfWeek       INT           NOT NULL,   -- 0=Sonntag … 6=Samstag (.NET DayOfWeek)
    StartTime       TIME          NOT NULL,   -- Ortszeit Zurich (07:00–23:00)
    EndTime         TIME          NOT NULL,
    ValidFrom       DATE          NOT NULL,
    ValidUntil      DATE          NOT NULL,
    IntervalWeeks   INT           NOT NULL DEFAULT 1,  -- 1 = wöchentlich, 2 = zweiwöchentlich
    IsActive        BIT           NOT NULL DEFAULT 1,
    ExcludeSchoolHolidays BIT     NOT NULL DEFAULT 0,  -- 1 = Generator überspringt Feriendaten
    Color           NVARCHAR(7)   NULL,      -- Hex-Farbcode, z.B. '#4A86E8'; NULL = Standardfarbe
    Notes           NVARCHAR(MAX) NULL,
    CreatedAt       DATETIME2     NOT NULL,
    CreatedBy       NVARCHAR(200) NOT NULL
)
```

#### `BookingAuditLog` — Unveränderliches Audit-Protokoll

```sql
CREATE TABLE BookingAuditLog (
    Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
    EntityType      NVARCHAR(50)  NOT NULL,  -- 'BookingSlot' | 'RecurringRule' | 'Member'
    EntityId        INT           NOT NULL,
    Action          NVARCHAR(50)  NOT NULL,  -- 'Created' | 'Confirmed' | 'Rejected' | 'Cancelled' | 'PriceChanged' | 'Deleted'
    ChangedBy       NVARCHAR(200) NOT NULL,  -- E-Mail oder 'admin:<UmbracoUser>'
    ChangedAt       DATETIME2     NOT NULL,
    OldStatusJson   NVARCHAR(MAX) NULL,      -- JSON-Snapshot vor der Änderung
    NewStatusJson   NVARCHAR(MAX) NULL,      -- JSON-Snapshot nach der Änderung
    RemoteIp        NVARCHAR(45)  NULL,
    Notes           NVARCHAR(500) NULL
)
```

#### `SchoolHolidays` — Schulferien (für Serienausnahmen)

```sql
CREATE TABLE SchoolHolidays (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,   -- z.B. "Sommerferien 2027"
    HolidayFrom     DATE          NOT NULL,
    HolidayUntil    DATE          NOT NULL,   -- inklusiv
    CreatedAt       DATETIME2     NOT NULL,

    CONSTRAINT CK_SchoolHolidays_Range CHECK (HolidayUntil >= HolidayFrom)
)
```

---

### 1.2 Domain-Schicht

#### Value Objects

```csharp
public record TimeSlot
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public TimeSlot(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("TimeSlot muss UTC-Werte enthalten.");
        if (endUtc <= startUtc)
            throw new DomainException("EndUtc muss nach StartUtc liegen.");
        if ((endUtc - startUtc).TotalMinutes < 30)
            throw new DomainException("Mindestbuchungsdauer beträgt 30 Minuten.");
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public int BlockCount(int blockMinutes = 30) =>
        (int)((EndUtc - StartUtc).TotalMinutes / blockMinutes);

    public bool OverlapsWith(TimeSlot other) =>
        StartUtc < other.EndUtc && EndUtc > other.StartUtc;
}

public record RenterEmail
{
    public string Value { get; }
    public RenterEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new DomainException("Ungültige E-Mail-Adresse.");
        Value = value.Trim().ToLowerInvariant();
    }
}

public enum RenterTypeValue { Verein, Firma, Privatperson }
public record RenterType
{
    public RenterTypeValue Value { get; }
    public RenterType(string raw) =>
        Value = Enum.TryParse<RenterTypeValue>(raw, out var v)
            ? v
            : throw new DomainException($"Unbekannter Mietertyp: {raw}");
}

public enum BookingStatusValue { Provisorisch, Bestätigt, Storniert }
public record BookingStatus
{
    public BookingStatusValue Value { get; }
    public static readonly BookingStatus Provisional = new(BookingStatusValue.Provisorisch);
    public static readonly BookingStatus Confirmed   = new(BookingStatusValue.Bestätigt);
    public static readonly BookingStatus Cancelled   = new(BookingStatusValue.Storniert);

    private BookingStatus(BookingStatusValue v) => Value = v;
    public static BookingStatus FromString(string s) => s switch
    {
        "Provisorisch" => Provisional,
        "Bestätigt"    => Confirmed,
        "Storniert"    => Cancelled,
        _              => throw new DomainException($"Unbekannter Buchungsstatus: {s}")
    };
    public override string ToString() => Value.ToString();
}
```

#### Aggregate Root `BookingSlot`

```csharp
public sealed class BookingSlot
{
    public int Id { get; private set; }
    public int? MemberId { get; private set; }  // Umbraco IMember.Id
    public int? RecurringRuleId { get; private set; }
    public BookingStatus Status { get; private set; }
    public TimeSlot Slot { get; private set; }
    public decimal? PricePerBlock { get; private set; }
    public int? TotalBlocks { get; private set; }
    public decimal? TotalPrice { get; private set; }
    public string? PriceNote { get; private set; }
    public bool IsRecurringSlot { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "";

    private BookingSlot() { }

    public static BookingSlot CreateUserBooking(
        int memberId, TimeSlot slot, decimal pricePerBlock, string createdBy, string? notes)
    {
        var blocks = slot.BlockCount();
        return new BookingSlot
        {
            MemberId = memberId,
            Status = BookingStatus.Provisional,
            Slot = slot,
            PricePerBlock = pricePerBlock,
            TotalBlocks = blocks,
            TotalPrice = pricePerBlock * blocks,
            IsRecurringSlot = false,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public static BookingSlot CreateRecurringSlot(
        int? memberId, int recurringRuleId, TimeSlot slot, string createdBy, string? color)
    {
        return new BookingSlot
        {
            MemberId = memberId,
            RecurringRuleId = recurringRuleId,
            Status = BookingStatus.Confirmed,  // Serientermine sofort bestätigt
            Slot = slot,
            PricePerBlock = null,
            TotalBlocks = null,
            TotalPrice = null,
            IsRecurringSlot = true,
            Color = color,                     // vererbt von RecurringRule
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void Confirm()
    {
        if (Status != BookingStatus.Provisional)
            throw new DomainException("Nur provisorische Buchungen können bestätigt werden.");
        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != BookingStatus.Provisional)
            throw new DomainException("Nur provisorische Buchungen können abgelehnt werden.");
        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Buchung ist bereits storniert.");
        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustPrice(decimal newPricePerBlock, string? note)
    {
        if (IsRecurringSlot)
            throw new DomainException("Serienbuchungen haben keinen Einzelpreis.");
        PricePerBlock = newPricePerBlock;
        TotalPrice = newPricePerBlock * (TotalBlocks ?? 0);
        PriceNote = note;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

#### `HallMember` — leichtgewichtiges Record (wraps Umbraco IMember)

`HallRenter` als Aggregate Root entfällt. Stattdessen werden Mieterdaten direkt via
`IMemberManagerPort` aus Umbraco Members gelesen. Für Read-Operationen (E-Mail-Versand,
CSV-Export) gibt es ein schlankes `HallMember`-Record:

```csharp
public sealed record HallMember(
    int     Id,
    string  Email,
    string  ContactPerson,       // = Member.Name
    RenterType RenterType,
    string  BillingName,
    string  BillingAddress,
    string  BillingPostalCode,
    string  BillingCity,
    string  BillingCountry,
    string? Phone,
    bool    HasKey,
    bool    HasPassword,         // = Member.HasPassword (Identity)
    DateTime? MagicLinkSentAt,
    DateTime? PasswordResetSentAt
);
```

#### Outbound Port `IMemberManagerPort`

Wraps `IMemberManager` und `SignInManager<MemberIdentityUser>` für Testbarkeit:

```csharp
public interface IMemberManagerPort
{
    Task<HallMember?> FindByEmailAsync(string email);
    Task<HallMember?> FindByIdAsync(int memberId);
    Task<HallMember> CreateAsync(RegisterRenterCommand cmd, string? password);
    Task UpdateProfileAsync(int memberId, string contactPerson, string billingName,
        string billingAddress, string billingPostalCode, string billingCity, string? phone, bool hasKey);

    // Auth
    Task<bool> CheckPasswordAsync(string email, string password);
    Task SignInAsync(int memberId);
    Task SignOutAsync();
    Task AddOrChangePasswordAsync(int memberId, string newPassword);

    // Identity password-reset (TOTP-basiert, kein eigener Token-Store nötig)
    Task<string> GeneratePasswordResetTokenAsync(int memberId);
    Task ResetPasswordAsync(int memberId, string token, string newPassword);

    // Rate-Limit-Felder (Custom Member Properties)
    Task<DateTime?> GetMagicLinkSentAtAsync(int memberId);
    Task SetMagicLinkSentAtAsync(int memberId, DateTime sentAt);
    Task<DateTime?> GetPasswordResetSentAtAsync(int memberId);
    Task SetPasswordResetSentAtAsync(int memberId, DateTime sentAt);
}

public interface IBookingSlotRepository
{
    Task<IReadOnlyList<BookingSlot>> GetForWeekAsync(DateTime mondayUtc, DateTime sundayUtc);
    Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsAsync(TimeSlot slot);
    Task<BookingSlot?> FindByIdAsync(int id);
    Task<BookingSlot> SaveAsync(BookingSlot slot);
    Task UpdateAsync(BookingSlot slot);
    Task<IReadOnlyList<BookingSlot>> GetForMemberAsync(int memberId);
    Task<IReadOnlyList<BookingSlot>> GetForExportAsync(
        DateTime fromUtc, DateTime toUtc, bool confirmedOnly);
    Task<IReadOnlyList<BookingSlot>> GetPendingAdminApprovalAsync();
    Task SaveBatchAsync(IReadOnlyList<BookingSlot> slots);
}

public interface IRecurringRuleRepository
{
    Task<RecurringRule> SaveAsync(RecurringRule rule);
    Task<IReadOnlyList<RecurringRule>> GetActiveRulesAsync();
    Task<RecurringRule?> FindByIdAsync(int id);
    Task DeactivateAsync(int id);
}

public interface IMagicLinkTokenRepository
{
    Task SaveAsync(MagicLinkToken token);
    Task<MagicLinkToken?> FindByHashAsync(string tokenHash);
    Task MarkUsedAsync(int tokenId);
    Task PurgeExpiredAsync();
}

public interface IBookingAuditRepository
{
    Task LogAsync(string entityType, int entityId, string action,
                  string changedBy, object? oldState, object? newState,
                  string? remoteIp = null, string? notes = null);
}

public interface IBookingEmailPort
{
    Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member);
    Task SendAdminNewBookingNotificationAsync(BookingSlot slot, HallMember member);
    Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member);
    Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member);
    // Registrierungsbestätigung enthält bereits den ersten Magic Link (E-Mail-Sparsamkeit)
    Task SendRegistrationConfirmationWithMagicLinkAsync(HallMember member, string magicLinkUrl);
    Task SendMagicLinkAsync(HallMember member, string magicLinkUrl);
    Task SendPasswordResetAsync(HallMember member, string resetUrl);
}

public interface IBookingCsvPort
{
    byte[] ExportBookings(IReadOnlyList<(BookingSlot Slot, HallMember? Member)> data,
                          DateTime from, DateTime to);
}
```

---

## Phase 2: Backend-Logik & API

### 2.1 Authentifizierung

Alle Auth-Use-Cases delegieren an `IMemberManagerPort`, der `IMemberManager` und
`SignInManager<MemberIdentityUser>` (Umbraco / ASP.NET Core Identity) kapselt.
Cookie-Lebensdauer und Lockout-Schwellen sind via `IdentityOptions` konfigurierbar.

#### `SendMagicLinkUseCase`

```csharp
public sealed class SendMagicLinkUseCase(
    IMemberManagerPort members,
    IMagicLinkTokenRepository tokenRepo,
    IBookingEmailPort email)
{
    // Gibt false zurück, wenn der Nutzer noch nicht registriert ist (FE zeigt Reg-Formular)
    public async Task<bool> ExecuteAsync(string emailRaw, string? remoteIp)
    {
        var member = await members.FindByEmailAsync(emailRaw.Trim().ToLowerInvariant());
        if (member is null) return false;

        var lastSent = await members.GetMagicLinkSentAtAsync(member.Id);
        if (lastSent.HasValue && (DateTime.UtcNow - lastSent.Value).TotalMinutes < 10)
            throw new DomainException("Bitte warte 10 Minuten bevor du einen neuen Link anforderst.");

        var (plainToken, tokenHash) = GenerateToken();
        var magicLink = $"https://www.sporthalle-sulzerallee.ch/reservierung/auth?token={plainToken}";

        await tokenRepo.SaveAsync(MagicLinkToken.Create(member.Id, tokenHash, remoteIp));
        await members.SetMagicLinkSentAtAsync(member.Id, DateTime.UtcNow);
        await email.SendMagicLinkAsync(member, magicLink);

        return true;
    }

    private static (string plain, string hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = Base64UrlTextEncoder.Encode(bytes);
        var hash  = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain)));
        return (plain, hash);
    }
}
```

#### `ValidateMagicLinkUseCase`

```csharp
public sealed class ValidateMagicLinkUseCase(
    IMagicLinkTokenRepository tokenRepo,
    IMemberManagerPort members)
{
    public async Task<HallMember> ExecuteAsync(string plainToken)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));

        var token = await tokenRepo.FindByHashAsync(hash)
            ?? throw new DomainException("Ungültiger Link.");

        if (token.UsedAt.HasValue)
            throw new DomainException("Dieser Link wurde bereits verwendet.");
        if (token.ExpiresAt < DateTime.UtcNow)
            throw new DomainException("Der Link ist abgelaufen (Gültigkeitsdauer: 20 Minuten).");

        await tokenRepo.MarkUsedAsync(token.Id);

        var member = await members.FindByIdAsync(token.MemberId)
            ?? throw new DomainException("Mieter nicht gefunden.");

        // SignInAsync nutzt SignInManager<MemberIdentityUser> — setzt Identity Cookie
        await members.SignInAsync(member.Id);
        return member;
    }
}
```

Der MVC-Controller ruft `ValidateMagicLinkUseCase` auf und redirectet auf
`/reservierung?session=confirmed`. Kein eigener Cookie-Code im Controller nötig.

#### `RegisterRenterUseCase`

```csharp
public sealed class RegisterRenterUseCase(
    IMemberManagerPort members,
    IMagicLinkTokenRepository tokenRepo,
    ICaptchaPort captcha,
    IBookingEmailPort email)
{
    public async Task ExecuteAsync(RegisterRenterCommand cmd, string? captchaToken, string? remoteIp)
    {
        if (!await captcha.VerifyAsync(captchaToken, remoteIp))
            throw new DomainException("CAPTCHA-Überprüfung fehlgeschlagen.");

        if (await members.FindByEmailAsync(cmd.Email) is not null)
            throw new DomainException("Diese E-Mail-Adresse ist bereits registriert.");

        // Optionales Passwort: Identity (BCrypt) übernimmt das Hashing
        var member = await members.CreateAsync(cmd, cmd.Password);

        // Registrierungsbestätigung enthält bereits den ersten Magic Link
        var (plainToken, tokenHash) = GenerateToken();
        var magicLink = $"https://www.sporthalle-sulzerallee.ch/reservierung/auth?token={plainToken}";
        await tokenRepo.SaveAsync(MagicLinkToken.Create(member.Id, tokenHash, remoteIp));
        await members.SetMagicLinkSentAtAsync(member.Id, DateTime.UtcNow);

        await email.SendRegistrationConfirmationWithMagicLinkAsync(member, magicLink);
    }

    private static (string plain, string hash) GenerateToken() { /* identisch wie SendMagicLinkUseCase */ }
}
```

#### `LoginWithPasswordUseCase`

```csharp
public sealed class LoginWithPasswordUseCase(IMemberManagerPort members)
{
    public async Task<HallMember> ExecuteAsync(string email, string password)
    {
        var member = await members.FindByEmailAsync(email)
            ?? throw new DomainException("Unbekannte E-Mail-Adresse.");

        if (!await members.CheckPasswordAsync(email, password))
            // Identity erhöht AccessFailedCount + lockt nach N Versuchen aus (IdentityOptions)
            throw new DomainException("Passwort ungültig.");

        await members.SignInAsync(member.Id);
        return member;
    }
}
```

#### `SetPasswordUseCase`

```csharp
public sealed class SetPasswordUseCase(IMemberManagerPort members)
{
    // Nutzer ist bereits per Magic Link eingeloggt und setzt jetzt ein Passwort
    public async Task ExecuteAsync(int memberId, string newPassword)
    {
        if (newPassword.Length < 8)
            throw new DomainException("Das Passwort muss mindestens 8 Zeichen lang sein.");
        await members.AddOrChangePasswordAsync(memberId, newPassword);
    }
}
```

#### `RequestPasswordResetUseCase`

```csharp
public sealed class RequestPasswordResetUseCase(
    IMemberManagerPort members,
    IBookingEmailPort email)
{
    public async Task ExecuteAsync(string emailRaw)
    {
        var member = await members.FindByEmailAsync(emailRaw);
        if (member is null) return;  // Stilles Ignorieren (Anti-Enumeration)

        var lastSent = await members.GetPasswordResetSentAtAsync(member.Id);
        if (lastSent.HasValue && (DateTime.UtcNow - lastSent.Value).TotalMinutes < 10)
            return;

        // Identity generiert TOTP-Token (60 Min. gültig, konfigurierbar via IdentityOptions)
        var resetToken = await members.GeneratePasswordResetTokenAsync(member.Id);
        var resetUrl = $"https://www.sporthalle-sulzerallee.ch/reservierung/reset-password" +
                       $"?token={Uri.EscapeDataString(resetToken)}&id={member.Id}";

        await members.SetPasswordResetSentAtAsync(member.Id, DateTime.UtcNow);
        await email.SendPasswordResetAsync(member, resetUrl);
    }
}
```

#### `ResetPasswordUseCase`

```csharp
public sealed class ResetPasswordUseCase(IMemberManagerPort members)
{
    public async Task ExecuteAsync(int memberId, string token, string newPassword)
    {
        if (newPassword.Length < 8)
            throw new DomainException("Das Passwort muss mindestens 8 Zeichen lang sein.");
        // IMemberManager.ResetPasswordAsync prüft TOTP-Token (Ablauf + Einmaligkeit via Identity)
        await members.ResetPasswordAsync(memberId, token, newPassword);
    }
}
```

### 2.2 Buchungslogik & Konfliktprüfung

#### `CreateBookingUseCase`

```csharp
public sealed class CreateBookingUseCase(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members,
    IBookingAuditRepository audit,
    IBookingEmailPort email,
    IHallConfigurationPort config)  // Umbraco-Konfig
{
    public async Task<BookingSlot> ExecuteAsync(CreateBookingCommand cmd)
    {
        var member = await members.FindByIdAsync(cmd.MemberId)
            ?? throw new DomainException("Mieter nicht gefunden.");

        var slot = new TimeSlot(cmd.StartUtc, cmd.EndUtc);

        // Konfliktprüfung mit SELECT … WITH (UPDLOCK, HOLDLOCK) in der Repository-Impl.
        var overlaps = await slotRepo.GetActiveOverlapsAsync(slot);
        if (overlaps.Count > 0)
            throw new SlotConflictException(slot, overlaps);

        var pricePerBlock = await config.GetPricePerBlockAsync();
        var booking = BookingSlot.CreateUserBooking(
            cmd.MemberId, slot, pricePerBlock,
            createdBy: member.Email, cmd.Notes);

        await slotRepo.SaveAsync(booking);

        await audit.LogAsync("BookingSlot", booking.Id, "Created",
            member.Email, null, new { booking.Status, slot.StartUtc, slot.EndUtc });

        // Provisorische Bestätigung an Mieter + Admin-Benachrichtigung
        await email.SendProvisionConfirmationToRenterAsync(booking, member);
        await email.SendAdminNewBookingNotificationAsync(booking, member);

        return booking;
    }
}
```

**Transaktionale Konfliktprüfung** im Repository:

```csharp
public async Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsAsync(TimeSlot slot)
{
    // WITH (UPDLOCK, HOLDLOCK) serialisiert gleichzeitige Buchungsversuche.
    // Ohne dies könnten zwei parallele Requests beide "kein Konflikt" sehen.
    const string sql = @"
        SELECT * FROM BookingSlots WITH (UPDLOCK, HOLDLOCK)
        WHERE Status <> 'Storniert'
          AND StartUtc < @0
          AND EndUtc   > @1";
    return (await _db.FetchAsync<BookingSlotRecord>(sql, slot.EndUtc, slot.StartUtc))
        .Select(MapToDomain).ToList();
}
```

### 2.3 Seriengenerator

#### `CreateRecurringRuleUseCase`

```csharp
public sealed class CreateRecurringRuleUseCase(
    IRecurringRuleRepository ruleRepo,
    IBookingSlotRepository slotRepo,
    IBookingAuditRepository audit)
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<RecurringRule> ExecuteAsync(CreateRecurringRuleCommand cmd, string adminUser)
    {
        var rule = RecurringRule.Create(cmd, adminUser);
        await ruleRepo.SaveAsync(rule);

        var slots = GenerateSlots(rule, adminUser);

        // Bestehende Konflikte loggen, aber nicht blockieren (Admin entscheidet bewusst)
        var conflicts = new List<(TimeSlot slot, IReadOnlyList<BookingSlot> overlaps)>();
        foreach (var slot in slots)
        {
            var overlaps = await slotRepo.GetActiveOverlapsAsync(slot.Slot);
            if (overlaps.Count > 0) conflicts.Add((slot.Slot, overlaps));
        }

        await slotRepo.SaveBatchAsync(slots);

        await audit.LogAsync("RecurringRule", rule.Id, "Created", adminUser, null,
            new { rule.Description, SlotsGenerated = slots.Count, ConflictCount = conflicts.Count });

        return rule;
    }

    private static List<BookingSlot> GenerateSlots(
        RecurringRule rule, string adminUser, IReadOnlyList<SchoolHoliday> holidays)
    {
        var result  = new List<BookingSlot>();
        var current = rule.ValidFrom;
        int weekIndex = 0;  // für IntervalWeeks-Zählung

        while (current <= rule.ValidUntil)
        {
            if (current.DayOfWeek == (DayOfWeek)rule.DayOfWeek)
            {
                // Zweiwöchentlich: nur jede rule.IntervalWeeks-te Woche
                if (weekIndex % rule.IntervalWeeks == 0)
                {
                    // Schulferien überspringen wenn Flag gesetzt
                    bool isHoliday = rule.ExcludeSchoolHolidays &&
                        holidays.Any(h => current >= h.HolidayFrom && current <= h.HolidayUntil);

                    if (!isHoliday)
                    {
                // Ortszeit → UTC
                var startLocal = current.ToDateTime(rule.StartTime);
                var endLocal   = current.ToDateTime(rule.EndTime);
                var startUtc   = TimeZoneInfo.ConvertTimeToUtc(startLocal, Zurich);
                var endUtc     = TimeZoneInfo.ConvertTimeToUtc(endLocal, Zurich);

                        result.Add(BookingSlot.CreateRecurringSlot(
                            rule.RenterId, rule.Id, new TimeSlot(startUtc, endUtc),
                            adminUser, rule.Color));
                    }
                }
                weekIndex++;
            }
            current = current.AddDays(1);
        }
        return result;
    }
}
```

Ausnahmen (z.B. Schulferien) werden durch direktes Löschen oder Stornieren einzelner
`BookingSlot`-Einträge via `ReservierungAdminController` gelöst. Kein separates
Ausnahme-Modell notwendig.

### 2.4 API-Endpunkte

```
// Öffentlich
GET  /api/reservierung/wochen-slots?von=YYYY-MM-DD
     → WeekSlotResponse[] (Status, Start, End, Color — kein Mietername öffentlich)
     Cached: 30 Sekunden (Output-Cache), wird bei Buchungsänderung invalidiert

GET  /api/reservierung/verfuegbare-tage?monat=2026-11&dauernMinuten=60
     → { "dates": ["2026-11-03", "2026-11-10", "2026-11-18", …] }
     Nur Tage, an denen mindestens ein zusammenhängender freier Block der Länge `dauernMinuten`
     innerhalb der Öffnungszeiten existiert. Cached: 60 Sekunden.

GET  /api/reservierung/verfuegbare-slots?datum=2026-11-18&dauernMinuten=60
     → SlotOption[]
     SlotOption: { startUtc, endUtc, startLocal, endLocal, isAvailable, priceTotal }
     Listet alle möglichen Startzeiten des Tages (im Block-Raster) und markiert jede
     als verfügbar oder belegt. Cached: 10 Sekunden.

POST /api/reservierung/magic-link
     Body: { "email": "..." }
     → { "isKnownUser": true }  // false = FE zeigt Registrierungsformular

POST /api/reservierung/register
     Body: RegisterRenterRequest + CaptchaToken
     → 200 | 409 (E-Mail bereits registriert) | 400
     Legt Mieter an, sendet Magic Link

POST /api/reservierung/auth/validate
     Body: { "token": "..." }
     → Validiert Magic Link, setzt Identity Cookie | 400 (abgelaufen/ungültig)

POST /api/reservierung/auth/login
     Body: { "email": "...", "password": "..." }
     → 200 (Identity Cookie gesetzt) | 401 (ungültig) | 423 (Account gesperrt)

POST /api/reservierung/auth/logout
     → Identity Cookie löschen (SignOutAsync)

GET  /api/reservierung/me
     → HallMember-Daten (Email, Name, RenterType, HasPassword — nur für eingeloggte Nutzer)

POST /api/reservierung/me/password
     Body: { "newPassword": "..." }
     → 204 | 401

POST /api/reservierung/auth/forgot-password
     Body: { "email": "..." }
     → 204 (stilles OK, auch wenn E-Mail unbekannt — Anti-Enumeration)

POST /api/reservierung/auth/reset-password
     Body: { "memberId": 42, "token": "...", "newPassword": "..." }
     → 204 | 400 (Token abgelaufen/ungültig)

POST /api/reservierung/buchungen
     Body: CreateBookingRequest (StartUtc, EndUtc, Notes)
     Auth: Identity Cookie (Umbraco Member)
     → BookingSlot | 409 Conflict | 401 Unauthorized

GET  /api/reservierung/meine-buchungen
     Auth: Identity Cookie (Umbraco Member)
     → BookingSlot[] des eingeloggten Mieters

// Admin (UmbracoAdminAuthorize)
GET  /api/reservierung/admin/pendente
     → Alle BookingSlots mit Status 'Provisorisch'

POST /api/reservierung/admin/buchungen/{id}/bestaetigen
     → 204

POST /api/reservierung/admin/buchungen/{id}/ablehnen
     Body: { "reason": "..." }
     → 204

DELETE /api/reservierung/admin/buchungen/{id}
     → 204 (Storniert + Audit-Eintrag)

POST /api/reservierung/admin/buchungen/{id}/preis
     Body: { "pricePerBlock": 25.00, "note": "Vereinssonderpreis" }
     → 204

POST /api/reservierung/admin/serien-regeln
     Body: CreateRecurringRuleCommand
     → RecurringRule

GET  /api/reservierung/admin/export/csv?von=YYYY-MM-DD&bis=YYYY-MM-DD&nurBestaetigt=true
     → text/csv (UTF-8 mit BOM)
```

---

## Phase 3: Umbraco-Integration

### 3.1 Konfigurations-Knoten `ReservierungKonfiguration`

Ein nicht-öffentlicher Umbraco-Knoten (kein Template, `isElement: false`, nur unter Root erlaubt)
dient als Konfigurations-Datenspeicher. Der `IHallConfigurationPort` liest diese Werte zur Laufzeit.

**Content Type Alias:** `reservierungKonfiguration`

| Eigenschaft | Alias | Typ | Default |
|------------|-------|-----|---------|
| Öffnungszeit von | `openingHourStart` | Integer | 7 |
| Öffnungszeit bis | `openingHourEnd` | Integer | 23 |
| Blockgrösse (Min.) | `blockDurationMinutes` | Integer | 30 |
| Preis pro Block (CHF) | `pricePerBlock` | Decimal | 0 |
| Max. Wochen im Voraus | `maxWeeksAhead` | Integer | 12 |
| Buchbare Dauern (Min.) | `buchbareDauern` | TextBox | "30,60,90,120" |
| Anlass-Optionen | `anlasseOptionen` | TextBox | "Training,Turnier,Schulung,Privat,Sonstiges" |
| Buchungsregeln (Text) | `bookingRules` | Textarea | "" |

```csharp
public interface IHallConfigurationPort
{
    Task<decimal> GetPricePerBlockAsync();
    Task<int> GetBlockDurationMinutesAsync();
    Task<int> GetOpeningHourStartAsync();
    Task<int> GetOpeningHourEndAsync();
    Task<int> GetMaxWeeksAheadAsync();
    Task<IReadOnlyList<int>> GetBuchbareDauernAsync();   // z.B. [30, 60, 90, 120]
    Task<IReadOnlyList<string>> GetAnlasseAsync();       // z.B. ["Training","Turnier",…]
}

// Adapter: liest via IPublishedContentQuery aus dem Konfig-Knoten
public class UmbracoHallConfigurationAdapter(IPublishedContentQuery contentQuery)
    : IHallConfigurationPort
{
    private IPublishedContent Config =>
        contentQuery.ContentAtRoot().Descendants()
            .First(x => x.ContentType.Alias == "reservierungKonfiguration");

    public Task<decimal> GetPricePerBlockAsync() =>
        Task.FromResult(Config.Value<decimal>("pricePerBlock"));

    // analog für die anderen Werte
}
```

### 3.2 E-Mail-Templates via Brevo

Alle Mails nutzen Template ID 1 (bestehendes generisches Template mit
`params.SUBJECT`, `params.TITLE`, `params.FIRSTNAME`, `params.BODY`, `params.DETAILS`,
`params.CTA_URL`, `params.CTA_LABEL`).

#### Adapter-Muster (Auszug)

```csharp
public class BrevoBookingEmailAdapter(HttpClient http, IOptions<BrevoEmailOptions> opts)
    : IBookingEmailPort
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member)
    {
        var start = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, Zurich);
        var end   = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc,   Zurich);

        await SendAsync(member.Email,
            $"{member.ContactPerson.Split(' ')[0]}",
            subject:  "Deine Buchungsanfrage ist eingegangen",
            title:    "Buchung provisorisch reserviert",
            body:     "Deine Buchungsanfrage für die Sporthalle Sulzerallee ist eingegangen. " +
                      "Wir prüfen sie und bestätigen dir den Termin in Kürze per E-Mail.",
            details:  FormatDetails(start, end, slot),
            ctaUrl:   "https://www.sporthalle-sulzerallee.ch/reservierung",
            ctaLabel: "Meine Buchungen",
            bcc:      new[] { "jan.haug@sporthalle-sulzerallee.ch" });
    }

    public async Task SendMagicLinkAsync(HallMember member, string magicLinkUrl)
    {
        var firstName = member.ContactPerson.Split(' ')[0];
        await SendAsync(member.Email, firstName,
            subject:  "Dein Anmelde-Link für die Sporthalle",
            title:    "Anmelden ohne Passwort",
            body:     "Klicke auf den Button unten, um dich anzumelden. " +
                      "Der Link ist 20 Minuten gültig und kann nur einmal verwendet werden.",
            details:  $"Gültig bis: {DateTime.UtcNow.AddMinutes(20):dd.MM.yyyy HH:mm} Uhr (Zürich)",
            ctaUrl:   magicLinkUrl,
            ctaLabel: "Jetzt anmelden",
            bcc:      Array.Empty<string>());
    }

    // SendBookingConfirmedToRenterAsync, SendBookingRejectedToRenterAsync, etc. analog

    private static string FormatDetails(DateTime start, DateTime end, BookingSlot slot)
    {
        var price = slot.TotalPrice.HasValue
            ? $"CHF {slot.TotalPrice.Value:F2} ({slot.TotalBlocks}× 30 Min. à CHF {slot.PricePerBlock:F2})"
            : "Wird separat abgerechnet";
        return $"Datum: {start:dddd, dd. MMMM yyyy}\n" +
               $"Zeit: {start:HH:mm} – {end:HH:mm} Uhr\n" +
               $"Preis: {price}";
    }

    private async Task SendAsync(string toEmail, string firstName, string subject,
        string title, string body, string details, string ctaUrl, string ctaLabel,
        string[] bcc)
    {
        var payload = new
        {
            templateId = 1,
            to  = new[] { new { email = toEmail, name = firstName } },
            bcc = bcc.Select(e => new { email = e }).ToArray(),
            @params = new { SUBJECT = subject, TITLE = title, FIRSTNAME = firstName,
                            BODY = body, DETAILS = details, CTA_URL = ctaUrl, CTA_LABEL = ctaLabel }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", opts.Value.ApiKey);
        (await http.SendAsync(request)).EnsureSuccessStatusCode();
    }
}
```

### 3.3 uSync-Configs

**`reservierung.config`** (Content Type für die öffentliche Seite):
- Alias: `reservierung`
- Template: `Reservierung`
- Erlaubt unter `homePage`
- Eigenschaften: `pageHeading` (TextBox), `introText` (RichText)

**`reservierungKonfiguration.config`** (Konfigurations-Knoten):
- Alias: `reservierungKonfiguration`
- Kein Template (nur im Backoffice editierbar)
- Erlaubt: nur als Kind von Root (kein öffentlicher Auftritt)

### 3.4 `ReservierungComposer`

```csharp
public class ReservierungComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddComponent<ReservierungMigrationComponent>();

        builder.Services.Configure<BrevoEmailOptions>(builder.Config.GetSection("Brevo"));
        builder.Services.AddHttpClient<BrevoBookingEmailAdapter>();
        builder.Services.AddScoped<IBookingEmailPort, BrevoBookingEmailAdapter>();

        builder.Services.Configure<TurnstileOptions>(builder.Config.GetSection("Turnstile"));
        builder.Services.AddHttpClient<TurnstileCaptchaAdapter>();
        builder.Services.AddScoped<ICaptchaPort, TurnstileCaptchaAdapter>();

        // Member-Port: wraps IMemberManager + SignInManager (Umbraco Identity)
        builder.Services.AddScoped<IMemberManagerPort, UmbracoMemberAdapter>();

        builder.Services.AddScoped<IBookingSlotRepository, BookingSlotRepository>();
        builder.Services.AddScoped<IRecurringRuleRepository, RecurringRuleRepository>();
        builder.Services.AddScoped<IMagicLinkTokenRepository, MagicLinkTokenRepository>();
        builder.Services.AddScoped<IBookingAuditRepository, BookingAuditRepository>();
        builder.Services.AddSingleton<IBookingCsvPort, BookingCsvAdapter>();
        builder.Services.AddScoped<IHallConfigurationPort, UmbracoHallConfigurationAdapter>();

        builder.Services.AddScoped<SendMagicLinkUseCase>();
        builder.Services.AddScoped<ValidateMagicLinkUseCase>();
        builder.Services.AddScoped<RegisterRenterUseCase>();
        builder.Services.AddScoped<LoginWithPasswordUseCase>();
        builder.Services.AddScoped<SetPasswordUseCase>();
        builder.Services.AddScoped<RequestPasswordResetUseCase>();
        builder.Services.AddScoped<ResetPasswordUseCase>();
        builder.Services.AddScoped<CreateBookingUseCase>();
        builder.Services.AddScoped<ConfirmBookingUseCase>();
        builder.Services.AddScoped<RejectBookingUseCase>();
        builder.Services.AddScoped<CreateRecurringRuleUseCase>();
        builder.Services.AddScoped<GetWeekSlotsQuery>();
        builder.Services.AddScoped<BookingAdminService>();
    }
}
```

---

## Phase 4: Frontend (Blazor Server Components)

### 4.1 UX-Prinzipien

#### Anti-Bülach-Prinzip (Minimale Barriere)

Der Nutzer sieht sofort, was frei ist. Die Buchungsabsicht entsteht am Kalender, nicht
am Anfang eines mehrstufigen Formulars. Daraus folgen diese Design-Regeln:

1. **Kalender-First:** Der Wochenkalender ist das erste Element auf der Seite, ohne
   einleitendes Login-Wall. Freie Slots sind sofort erkennbar.
2. **Kein toter Ast:** Jeder Klick führt weiter. Wählt ein nicht eingeloggter Nutzer
   einen Slot, startet direkt der Magic-Link-Flow. Es gibt keine Seite, auf der der
   Nutzer im Nichts landet.
3. **Magic-Link statt Passwort:** Keine Passwort-Vergessen-Spirale. Eine E-Mail-Adresse
   genügt für den gesamten Flow.
4. **Buchungsanfrage vor Prüfung:** Der Nutzer sendet seine Anfrage, wir prüfen und
   melden uns. Keine Pre-Screening-Hürde vor dem ersten Kontakt.

#### Mobile-First-Strategie

Auf Geräten unter 768 px wechselt der Wochenkalender in eine Tagansicht (1 Spalte). In
`WochenkalenderComponent.razor` wird dies via `IsMobile`-State (gesetzt per JS-Interop
beim ersten Render) gesteuert — kein JS-basiertes DOM-Rewriting mehr.

- Navigation Tagansicht: `[‹ Gestern]  Mi, 18. Jun  [Morgen ›]`
- Swipe: JS-Interop-Stub (`reservierung.js`) registriert `touchstart`/`touchend` und
  ruft `DotNetReference.InvokeMethodAsync("NavigateDay", delta)` auf
- Mindest-Tap-Ziel: 44 × 44 px via CSS (`min-height: 44px` auf Blockzeilen)

#### Blazor-Setup in `Program.cs`

```csharp
// Nach builder.CreateUmbracoBuilder(...)
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();

// Im App-Pipeline-Block, nach app.UseUmbraco():
app.MapBlazorHub();
```

`_Imports.razor`:
```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using SporthalleWeb.Application.Reservierung
@using SporthalleWeb.Domain.Reservierung
```

### 4.2 `WochenkalenderComponent.razor`

Ersetzt das komplette Grid-Rendering aus `reservierung.js`. Lädt Wochen-Slots direkt via
`GetWeekSlotsQuery` (kein HTTP-Roundtrip). Zeigt Desktop-Wochenansicht und Mobile-Tagansicht.

**Farbcodierung:**

| Zustand | CSS-Klasse | Farbe |
|---------|-----------|-------|
| Frei | `slot-free` | Weiss / transparent |
| Vergangen | `slot-past` | Grau |
| Provisorisch | `slot-provisional` | Orange `#F5A623` |
| Bestätigt | `slot-confirmed` | Rot `#EB504B` |
| Serientermin mit Farbe | (inline style) | Admin-definierte Hex-Farbe |
| Serientermin ohne Farbe | `slot-recurring` | Dunkelgrau `#666` |

```razor
@rendermode InteractiveServer
@inject GetWeekSlotsQuery WeekQuery
@inject IHallConfigurationPort Config
@inject IJSRuntime JS

<div class="reservierung-app">

    @if (IsMobile)
    {
        <!-- Tagansicht -->
        <div class="day-nav">
            <button @onclick="() => NavigateDay(-1)">‹ Gestern</button>
            <span>@CurrentDay.ToString("dddd, d. MMMM", CultureInfo.GetCultureInfo("de-CH"))</span>
            <button @onclick="() => NavigateDay(+1)">Morgen ›</button>
        </div>
        <div class="day-grid" @ref="GridRef">
            @RenderDayBlocks(CurrentDay)
        </div>
    }
    else
    {
        <!-- Wochenansicht -->
        <div class="week-nav">
            <button @onclick="() => NavigateWeek(-1)">‹</button>
            <span>@WeekLabel</span>
            <button @onclick="() => NavigateWeek(+1)">›</button>
        </div>
        <div class="calendar-grid">
            <!-- Kopfzeile -->
            <div class="time-col"></div>
            @foreach (var day in WeekDays)
            {
                <div class="day-col @(day < Today ? "past" : "")">
                    @day.ToString("ddd d.M.", CultureInfo.GetCultureInfo("de-CH"))
                </div>
            }
            <!-- Zeitblöcke -->
            @for (var b = 0; b < TotalBlocks; b++)
            {
                var blockIndex = b;
                <div class="time-col">@(blockIndex % 2 == 0 ? BlockToTime(blockIndex) : "")</div>
                @foreach (var day in WeekDays)
                {
                    var slot = FindSlot(day, blockIndex);
                    <div class="grid-cell @GetSlotClass(slot, day)"
                         style="@GetInlineStyle(slot)">
                    </div>
                }
            }
        </div>
    }

    <button class="btn-book" @onclick="OpenPicker">Slot buchen</button>

    @if (ShowPicker)
    {
        <BuchungsPickerComponent
            OnClose="() => ShowPicker = false"
            OnSuccess="OnBookingCreated" />
    }

</div>

@code {
    private bool IsLoading = true;
    private bool IsMobile;
    private bool ShowPicker;
    private DateTime CurrentMonday = GetCurrentMonday();
    private DateTime CurrentDay = DateTime.Today;
    private DateTime Today = DateTime.Today;
    private IReadOnlyList<WeekSlotDto> WeekSlots = [];
    private int OpeningStart, OpeningEnd, BlockMinutes;
    private int TotalBlocks => (OpeningEnd - OpeningStart) * (60 / BlockMinutes);
    private IEnumerable<DateTime> WeekDays =>
        Enumerable.Range(0, 7).Select(i => CurrentMonday.AddDays(i));
    private string WeekLabel =>
        $"{CurrentMonday:d. MMM} – {CurrentMonday.AddDays(6):d. MMM yyyy}";
    private ElementReference GridRef;

    protected override async Task OnInitializedAsync()
    {
        OpeningStart  = await Config.GetOpeningHourStartAsync();
        OpeningEnd    = await Config.GetOpeningHourEndAsync();
        BlockMinutes  = await Config.GetBlockDurationMinutesAsync();
        await LoadWeekAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            IsMobile = await JS.InvokeAsync<bool>("eval", "window.innerWidth < 768");
            // Swipe-Listener für Mobile: JS-Stub gibt NavigateDay-Aufrufe zurück
            await JS.InvokeVoidAsync("reservierung.registerSwipe", GridRef,
                DotNetObjectReference.Create(this));
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void NavigateDay(int delta)
    {
        CurrentDay = CurrentDay.AddDays(delta);
        StateHasChanged();
    }

    private async Task NavigateWeek(int delta)
    {
        CurrentMonday = CurrentMonday.AddDays(delta * 7);
        await LoadWeekAsync();
    }

    private async Task LoadWeekAsync()
    {
        IsLoading = true;
        var endOfWeek = CurrentMonday.AddDays(7).ToUniversalTime();
        WeekSlots = await WeekQuery.GetForWeekAsync(
            CurrentMonday.ToUniversalTime(), endOfWeek);
        IsLoading = false;
    }

    private WeekSlotDto? FindSlot(DateTime day, int blockIndex)
    {
        // Blockindex → UTC-Startzeit berechnen und passenden Slot suchen
        var localStart = day.Date
            .AddHours(OpeningStart)
            .AddMinutes(blockIndex * BlockMinutes);
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart,
            TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time"));
        return WeekSlots.FirstOrDefault(s =>
            s.StartUtc <= utcStart && s.EndUtc > utcStart);
    }

    private string GetSlotClass(WeekSlotDto? slot, DateTime day)
    {
        if (day < Today) return "slot-past";
        if (slot is null) return "slot-free";
        if (slot.IsRecurringSlot && slot.Color is null) return "slot-recurring";
        return slot.Status switch
        {
            "Provisorisch" => "slot-provisional",
            "Bestätigt"    => "slot-confirmed",
            _              => ""
        };
    }

    private string GetInlineStyle(WeekSlotDto? slot) =>
        slot?.IsRecurringSlot == true && slot.Color is not null
            ? $"background-color:{slot.Color};opacity:0.85"
            : "";

    private RenderFragment RenderDayBlocks(DateTime day) => builder =>
    {
        for (var b = 0; b < TotalBlocks; b++)
        {
            var slot  = FindSlot(day, b);
            var cls   = GetSlotClass(slot, day);
            var style = GetInlineStyle(slot);
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", $"grid-cell {cls}");
            if (!string.IsNullOrEmpty(style))
                builder.AddAttribute(2, "style", style);
            builder.CloseElement();
        }
    };

    private string BlockToTime(int b) =>
        TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(OpeningStart * 60 + b * BlockMinutes))
                .ToString("HH:mm");

    private static DateTime GetCurrentMonday()
    {
        var today = DateTime.Today;
        var diff  = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return today.AddDays(-diff);
    }

    private void OpenPicker() => ShowPicker = true;

    private async Task OnBookingCreated()
    {
        ShowPicker = false;
        await LoadWeekAsync(); // Kalender sofort aktualisieren
    }
}
```

`wwwroot/js/reservierung.js` (stark reduziert, nur noch JS-Interop-Stubs):
```js
window.reservierung = {
  registerSwipe(element, dotnetRef) {
    let startX = 0;
    element.addEventListener('touchstart', e => { startX = e.changedTouches[0].screenX; }, { passive: true });
    element.addEventListener('touchend', e => {
      const delta = e.changedTouches[0].screenX - startX;
      if (Math.abs(delta) > 50)
        dotnetRef.invokeMethodAsync('NavigateDay', delta < 0 ? 1 : -1);
    });
  }
};
```

### 4.3 `BuchungsPickerComponent.razor`

Ersetzt den 4-Schritt-Picker vollständig. Ruft Application-Layer-Queries direkt auf.
Der Auth-Subflow (Magic Link, Cookie-Setzen) bleibt MVC-seitig; der Picker prüft den
Session-Status via `GET /api/reservierung/me` nach der Rückkehr vom Magic-Link.

**Schritt-Übersicht:**

```
┌───────────────────────────────────────────────────────┐
│  Schritt 1: Dauer wählen                              │
│  [ 30 Min.  CHF 25.– ]  [ 60 Min.  CHF 50.– ]       │
│  [ 90 Min.  CHF 75.– ]  [120 Min.  CHF 100.– ]      │
├───────────────────────────────────────────────────────┤
│  Schritt 2: Datum wählen (Monatskalender)             │
│  ← Juni 2026 →    verfügbare Tage klickbar           │
├───────────────────────────────────────────────────────┤
│  Schritt 3: Zeit wählen (Slot-Pills)                  │
│  [ 08:00–09:00 ]  [ 09:00–10:00 ]                   │
│    11:00–12:00 ← belegt                               │
├───────────────────────────────────────────────────────┤
│  Schritt 4: Bestätigen + Auth                         │
│  Anlass: [Training ▼]  Notiz: [_____]                │
│  E-Mail: [___]  → Magic-Link-Flow                    │
│  ☐ AGB    [Buchung anfragen]                          │
└───────────────────────────────────────────────────────┘
```

```razor
@rendermode InteractiveServer
@inject IHallConfigurationPort Config
@inject GetAvailableDaysQuery AvailDaysQuery
@inject GetAvailableTimeSlotsQuery AvailSlotsQuery
@inject HttpClient Http
@inject NavigationManager Nav

<div class="picker" aria-live="polite">
    <button class="picker__close" @onclick="OnClose">×</button>

    @switch (Step)
    {
        case PickerStep.Dauer:
            <h2>Wie lange?</h2>
            <div class="picker__duration-grid">
                @foreach (var min in BookableDurations)
                {
                    var price = (decimal)min / BlockMinutes * PricePerBlock;
                    <button class="picker-duration @(SelectedMinutes == min ? "picker-duration--active" : "")"
                            @onclick="() => SelectDurationAsync(min)">
                        @min Min. — CHF @price.ToString("F0").–
                    </button>
                }
            </div>
            break;

        case PickerStep.Datum:
            <h2>Wann?</h2>
            <div class="picker__month-nav">
                <button @onclick="() => NavigateMonthAsync(-1)">‹</button>
                <span>@CurrentMonth.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-CH"))</span>
                <button @onclick="() => NavigateMonthAsync(+1)">›</button>
            </div>
            <div class="picker__calendar" role="grid">
                @{
                    var daysInMonth = DateTime.DaysInMonth(CurrentMonth.Year, CurrentMonth.Month);
                    var firstDow = (int)new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1).DayOfWeek;
                    firstDow = firstDow == 0 ? 6 : firstDow - 1; // Mo = 0
                    for (var i = 0; i < firstDow; i++) { <div></div> }
                    for (var d = 1; d <= daysInMonth; d++)
                    {
                        var iso = $"{CurrentMonth.Year}-{CurrentMonth.Month:D2}-{d:D2}";
                        var avail = AvailableDates.Contains(iso);
                        <button class="picker-day @(avail ? "picker-day--available" : "picker-day--unavailable")
                                                  @(iso == SelectedDate ? "picker-day--selected" : "")"
                                disabled="@(!avail)"
                                @onclick="() => SelectDateAsync(iso)">
                            @d
                        </button>
                    }
                }
            </div>
            <div class="picker__actions">
                <button @onclick="() => Step = PickerStep.Dauer">Zurück</button>
            </div>
            break;

        case PickerStep.Zeit:
            <h2>@FormattedDate — @SelectedMinutes Min.</h2>
            <div class="picker__slot-grid">
                @foreach (var slot in AvailableSlots)
                {
                    @if (slot.IsAvailable)
                    {
                        <button class="picker-slot picker-slot--available @(SelectedSlot == slot ? "picker-slot--active" : "")"
                                @onclick="() => SelectSlot(slot)">
                            @slot.StartLocal – @slot.EndLocal
                        </button>
                    }
                    else
                    {
                        <span class="picker-slot picker-slot--taken">@slot.StartLocal – @slot.EndLocal</span>
                    }
                }
            </div>
            <div class="picker__actions">
                <button @onclick="() => Step = PickerStep.Datum">Zurück</button>
            </div>
            break;

        case PickerStep.Bestaetigen:
            <div class="picker__summary">
                <strong>@FormattedDate, @SelectedSlot!.StartLocal – @SelectedSlot.EndLocal Uhr</strong>
                <span>@SelectedMinutes Min. — CHF @TotalPrice.ToString("F2")</span>
            </div>

            <label>Anlass *
                <select @bind="EventType" required>
                    <option value="">Bitte wählen…</option>
                    @foreach (var anlass in EventTypes)
                    {
                        <option value="@anlass">@anlass</option>
                    }
                </select>
            </label>
            <label>Notiz (optional)
                <textarea @bind="Notes" rows="2"></textarea>
            </label>

            @if (IsLoggedIn)
            {
                <p class="session-info">Angemeldet als @SessionEmail</p>
                @if (ShowSetPasswordPrompt)
                {
                    <!-- Nach Magic-Link-Login: optionale Passwort-Einrichtung -->
                    <details class="set-password-prompt">
                        <summary>Passwort für nächstes Mal einrichten (optional)</summary>
                        <input type="password" @bind="NewPassword" placeholder="Mindestens 8 Zeichen" />
                        <button @onclick="SetPasswordAsync" disabled="@IsAuthBusy">
                            Passwort speichern
                        </button>
                        @if (PasswordSaved) { <span class="success">✓ Gespeichert</span> }
                    </details>
                }
            }
            else
            {
                <!-- Auth-Subflow: E-Mail eingeben → Passwort oder Magic Link -->
                <div class="picker__auth">
                    <label>Deine E-Mail-Adresse *
                        <input type="email" @bind="AuthEmail" @onblur="CheckEmailAsync" />
                    </label>

                    @if (MagicLinkSent)
                    {
                        <p class="info-box">Anmelde-Link gesendet an @AuthEmail. Bitte prüfe dein Postfach.</p>
                    }
                    else if (ShowRegistration)
                    {
                        <!-- Registrierungsformular (inline) -->
                        <div class="registration-form">
                            <!-- Felder: Mieter-Typ, Ansprechperson, Rechnungsadresse, … -->
                            <label>Passwort einrichten (optional)
                                <input type="password" @bind="RegistrationPassword"
                                       placeholder="Leer lassen = nur Magic-Link-Login" />
                            </label>
                            <button @onclick="RegisterAsync" disabled="@IsAuthBusy">
                                Registrieren & Anmelde-Link senden
                            </button>
                        </div>
                    }
                    else if (MemberHasPassword)
                    {
                        <!-- Bekannter Nutzer mit Passwort: Passwort-Login zuerst anbieten -->
                        <label>Passwort
                            <input type="password" @bind="LoginPassword" />
                        </label>
                        @if (AuthError is not null)
                        {
                            <p class="error-message" role="alert">@AuthError</p>
                        }
                        <div class="picker__auth-actions">
                            <button class="btn-primary" @onclick="LoginWithPasswordAsync"
                                    disabled="@IsAuthBusy">
                                Anmelden
                            </button>
                            <button class="btn-link" @onclick="SendMagicLinkAsync"
                                    disabled="@IsAuthBusy">
                                Magic Link anfordern
                            </button>
                        </div>
                    }
                    else
                    {
                        <!-- Bekannter Nutzer ohne Passwort: nur Magic Link -->
                        <button @onclick="SendMagicLinkAsync" disabled="@IsAuthBusy">
                            Anmelde-Link anfordern
                        </button>
                    }
                </div>
            }

            <label class="checkbox-label">
                <input type="checkbox" @bind="AgreedToTerms" />
                Ich stimme den <a href="/buchungsbedingungen">Buchungsbedingungen</a> zu.
            </label>

            @if (ErrorMessage is not null)
            {
                <p class="error-message" role="alert">@ErrorMessage</p>
            }

            <div class="picker__actions">
                <button @onclick="() => Step = PickerStep.Zeit">Zurück</button>
                <button class="btn-primary"
                        disabled="@(!CanSubmit)"
                        @onclick="SubmitBookingAsync">
                    @(IsSubmitting ? "Wird gesendet…" : "Buchung anfragen")
                </button>
            </div>
            break;

        case PickerStep.Bestaetigung:
            <div class="success-banner">
                <h2>Buchungsanfrage gesendet!</h2>
                <p>Wir prüfen deinen Termin und melden uns per E-Mail.</p>
                <button @onclick="ResetAsync">Weitere Buchung anfragen</button>
            </div>
            break;
    }
</div>

@code {
    private enum PickerStep { Dauer, Datum, Zeit, Bestaetigen, Bestaetigung }

    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnSuccess { get; set; }

    private PickerStep Step = PickerStep.Dauer;
    private IReadOnlyList<int> BookableDurations = [];
    private int BlockMinutes, OpeningStart;
    private decimal PricePerBlock;
    private IReadOnlyList<string> EventTypes = [];

    private int? SelectedMinutes;
    private DateOnly CurrentMonth = DateOnly.FromDateTime(DateTime.Today);
    private HashSet<string> AvailableDates = [];
    private string? SelectedDate;
    private IReadOnlyList<SlotOption> AvailableSlots = [];
    private SlotOption? SelectedSlot;

    private string? EventType, Notes, AuthEmail, ErrorMessage, SessionEmail;
    private string? LoginPassword, RegistrationPassword, NewPassword, AuthError;
    private bool IsLoggedIn, MagicLinkSent, ShowRegistration, AgreedToTerms;
    private bool IsAuthBusy, IsSubmitting, MemberHasPassword, ShowSetPasswordPrompt, PasswordSaved;

    private decimal TotalPrice =>
        SelectedMinutes.HasValue
            ? (decimal)SelectedMinutes.Value / BlockMinutes * PricePerBlock
            : 0;

    private string FormattedDate =>
        SelectedDate is not null
            ? DateOnly.Parse(SelectedDate).ToString("dddd, d. MMMM yyyy",
                CultureInfo.GetCultureInfo("de-CH"))
            : "";

    private bool CanSubmit => IsLoggedIn && !string.IsNullOrEmpty(EventType)
                              && AgreedToTerms && !IsSubmitting;

    protected override async Task OnInitializedAsync()
    {
        BookableDurations = await Config.GetBuchbareDauernAsync();
        BlockMinutes      = await Config.GetBlockDurationMinutesAsync();
        OpeningStart      = await Config.GetOpeningHourStartAsync();
        PricePerBlock     = await Config.GetPricePerBlockAsync();
        EventTypes        = await Config.GetAnlasseAsync();

        // Prüfen ob bereits eingeloggte Session vorhanden (Cookie vorhanden)
        var me = await Http.GetAsync("/api/reservierung/me");
        if (me.IsSuccessStatusCode)
        {
            var member = await me.Content.ReadFromJsonAsync<HallMemberDto>();
            IsLoggedIn    = true;
            SessionEmail  = member?.Email;
            MemberHasPassword = member?.HasPassword ?? false;
            ShowSetPasswordPrompt = !MemberHasPassword; // nur nach Magic-Link-Login sinnvoll
        }

        // Magic-Link-Rückkehr: ?session=confirmed in URL?
        var uri = new Uri(Nav.Uri);
        if (uri.Query.Contains("session=confirmed"))
        {
            IsLoggedIn = true;
            // Gespeicherten Slot-State wiederherstellen
            RestorePickerState();
        }
    }

    private async Task SelectDurationAsync(int minutes)
    {
        SelectedMinutes = minutes;
        await LoadAvailableDaysAsync();
        Step = PickerStep.Datum;
    }

    private async Task NavigateMonthAsync(int delta)
    {
        CurrentMonth = CurrentMonth.AddMonths(delta);
        await LoadAvailableDaysAsync();
    }

    private async Task LoadAvailableDaysAsync()
    {
        var monat = $"{CurrentMonth.Year}-{CurrentMonth.Month:D2}";
        var days  = await AvailDaysQuery.GetAsync(monat, SelectedMinutes!.Value);
        AvailableDates = [.. days];
    }

    private async Task SelectDateAsync(string iso)
    {
        SelectedDate   = iso;
        AvailableSlots = await AvailSlotsQuery.GetAsync(iso, SelectedMinutes!.Value);
        Step = PickerStep.Zeit;
    }

    private void SelectSlot(SlotOption slot)
    {
        SelectedSlot = slot;
        Step = PickerStep.Bestaetigen;
    }

    // Beim Verlassen des E-Mail-Felds: prüfen ob Nutzer bekannt ist + ob er ein Passwort hat
    private async Task CheckEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(AuthEmail)) return;
        var res = await Http.PostAsJsonAsync("/api/reservierung/magic-link",
            new { email = AuthEmail, checkOnly = true });
        if (res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadFromJsonAsync<MagicLinkCheckResponse>();
            MemberHasPassword = body?.HasPassword ?? false;
            ShowRegistration  = body?.IsKnownUser == false;
        }
    }

    private async Task SendMagicLinkAsync()
    {
        IsAuthBusy = true;
        SavePickerState();  // Slot-State für Rückkehr nach Magic-Link sichern
        var res = await Http.PostAsJsonAsync("/api/reservierung/magic-link",
            new { email = AuthEmail });
        var body = await res.Content.ReadFromJsonAsync<MagicLinkResponse>();
        if (body?.IsKnownUser == false)
            ShowRegistration = true;
        else
            MagicLinkSent = true;
        IsAuthBusy = false;
    }

    private async Task LoginWithPasswordAsync()
    {
        IsAuthBusy = true;
        AuthError  = null;
        var res = await Http.PostAsJsonAsync("/api/reservierung/auth/login",
            new { email = AuthEmail, password = LoginPassword });
        if (res.IsSuccessStatusCode)
        {
            IsLoggedIn = true;
            var member = await res.Content.ReadFromJsonAsync<HallMemberDto>();
            SessionEmail = member?.Email;
        }
        else
        {
            AuthError = res.StatusCode == System.Net.HttpStatusCode.Locked
                ? "Dein Konto ist gesperrt. Bitte setze dein Passwort zurück."
                : "Ungültiges Passwort.";
        }
        IsAuthBusy = false;
    }

    private async Task SetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPassword)) return;
        IsAuthBusy = true;
        var res = await Http.PostAsJsonAsync("/api/reservierung/me/password",
            new { newPassword = NewPassword });
        PasswordSaved = res.IsSuccessStatusCode;
        IsAuthBusy = false;
    }

    private async Task SubmitBookingAsync()
    {
        IsSubmitting = true;
        ErrorMessage = null;
        try
        {
            var res = await Http.PostAsJsonAsync("/api/reservierung/buchungen", new
            {
                startUtc      = SelectedSlot!.StartUtc,
                endUtc        = SelectedSlot.EndUtc,
                dauernMinuten = SelectedMinutes,
                eventType     = EventType,
                notes         = Notes
            });
            if (!res.IsSuccessStatusCode)
            {
                ErrorMessage = res.StatusCode == System.Net.HttpStatusCode.Conflict
                    ? "Dieser Slot wurde soeben von jemand anderem gebucht."
                    : "Fehler beim Senden der Buchungsanfrage.";
                return;
            }
            Step = PickerStep.Bestaetigung;
            await OnSuccess.InvokeAsync();
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task ResetAsync()
    {
        Step = PickerStep.Dauer;
        SelectedMinutes = null; SelectedDate = null; SelectedSlot = null;
        EventType = null; Notes = null; AgreedToTerms = false;
        await OnClose.InvokeAsync();
    }

    private void SavePickerState() { /* via ProtectedSessionStorage: Slot, Datum, Dauer */ }
    private void RestorePickerState() { /* Slot aus ProtectedSessionStorage wiederherstellen */ }
}
```

**Magic-Link-Rückkehr (bleibt MVC-seitig):**
`GET /reservierung/auth/validate?token=...` setzt den HttpOnly-Cookie und redirectet
zurück auf `/reservierung?session=confirmed`. Der `BuchungsPickerComponent` erkennt den
Query-Parameter und stellt den gespeicherten Slot-State wieder her.

### 4.4 Umbraco Template `Views/Reservierung.cshtml`

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
    @Model.Value("introText")
</div>

<!-- Blazor-Komponente: Wochenkalender + Buchungs-Picker -->
<component type="typeof(WochenkalenderComponent)" render-mode="Server" />

<!-- Legende -->
<div class="calendar-legend">
    <span class="slot-provisional">Provisorisch</span>
    <span class="slot-confirmed">Bestätigt</span>
    <span class="slot-recurring">Serientermin</span>
</div>

<!-- Blazor Hub + Turnstile -->
<script src="/_blazor"></script>
<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<script src="/js/reservierung.js"></script>
```

---

## Phase 5: Admin-Features (Blazor)

### 5.1 `ReservierungAdminComponent.razor`

Ersetzt `ReservierungAdmin.cshtml` inkl. aller AJAX-Snippets. Die Razor Page wird zum
dünnen Wrapper mit `[UmbracoAdminAuthorize]`. Die Komponente hat vier Tabs.

```razor
@rendermode InteractiveServer
@inject BookingAdminService AdminSvc
@inject ConfirmBookingUseCase ConfirmUseCase
@inject RejectBookingUseCase RejectUseCase
@inject CreateRecurringRuleUseCase RecurringUseCase
@inject IBookingSlotRepository SlotRepo
@inject IRecurringRuleRepository RuleRepo

<div class="admin-panel">
    <h1>Reservierungsverwaltung</h1>

    <!-- Tab-Navigation -->
    <div class="admin-tabs">
        @foreach (var tab in Tabs)
        {
            <button class="tab @(ActiveTab == tab ? "tab--active" : "")"
                    @onclick="() => ActiveTab = tab">
                @tab
                @if (tab == "Pendente" && PendingCount > 0)
                {
                    <span class="badge">@PendingCount</span>
                }
            </button>
        }
    </div>

    @switch (ActiveTab)
    {
        case "Pendente":
            <h2>Pendente Buchungen</h2>
            @if (!PendingSlots.Any())
            {
                <p>Keine pendenten Buchungen.</p>
            }
            else
            {
                <table class="admin-table">
                    <thead>
                        <tr><th>Datum</th><th>Zeit</th><th>Dauer</th><th>Mieter</th>
                            <th>Typ</th><th>CHF</th><th>Aktionen</th></tr>
                    </thead>
                    <tbody>
                        @foreach (var (slot, member) in PendingSlots)
                        {
                            var startLocal = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, Zurich);
                            var endLocal   = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc,   Zurich);
                            <tr>
                                <td>@startLocal.ToString("dd.MM.yyyy")</td>
                                <td>@startLocal.ToString("HH:mm") – @endLocal.ToString("HH:mm")</td>
                                <td>@((int)(slot.Slot.EndUtc - slot.Slot.StartUtc).TotalMinutes) Min.</td>
                                <td>@member?.ContactPerson</td>
                                <td>@member?.RenterType.Value</td>
                                <td>@slot.TotalPrice?.ToString("F2")</td>
                                <td>
                                    <button class="btn-confirm" @onclick="() => ConfirmAsync(slot.Id)">
                                        Bestätigen
                                    </button>
                                    <button class="btn-reject" @onclick="() => OpenRejectDialog(slot.Id)">
                                        Ablehnen
                                    </button>
                                    <div class="price-inline">
                                        <input type="number" step="0.50"
                                               @bind="PriceBuffer[slot.Id]" />
                                        <button @onclick="() => AdjustPriceAsync(slot.Id)">Preis</button>
                                    </div>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            }
            break;

        case "Alle Buchungen":
            <div class="week-selector">
                <button @onclick="() => AdminWeek = AdminWeek.AddDays(-7)">‹</button>
                <span>KW @ISOWeek.GetWeekOfYear(AdminWeek): @AdminWeek.ToString("d. MMM") –
                      @AdminWeek.AddDays(6).ToString("d. MMM yyyy")</span>
                <button @onclick="() => AdminWeek = AdminWeek.AddDays(7)">›</button>
                <button @onclick="LoadWeekSlotsAsync">Laden</button>
            </div>
            <!-- Wochentabelle mit allen Slots + Statusfarben -->
            break;

        case "Serien-Regeln":
            <h2>Bestehende Serien-Regeln</h2>
            @foreach (var rule in RecurringRules)
            {
                <div class="rule-card">
                    <span class="color-pill" style="background:@(rule.Color ?? "#666")"></span>
                    <strong>@rule.Description</strong>
                    <span>@((DayOfWeek)rule.DayOfWeek), @rule.StartTime – @rule.EndTime</span>
                    <span>@rule.ValidFrom.ToString("dd.MM.yy") – @rule.ValidUntil.ToString("dd.MM.yy")</span>
                    <button @onclick="() => DeactivateRuleAsync(rule.Id)">Deaktivieren</button>
                </div>
            }
            <h2>Neue Serien-Regel</h2>
            <EditForm Model="NewRule" OnValidSubmit="CreateRecurringRuleAsync">
                <label>Beschreibung *<InputText @bind-Value="NewRule.Description" /></label>
                <label>Wochentag *
                    <InputSelect @bind-Value="NewRule.DayOfWeek">
                        @foreach (var dow in Enum.GetValues<DayOfWeek>())
                        {
                            <option value="@(int)dow">@dow</option>
                        }
                    </InputSelect>
                </label>
                <div class="form-row">
                    <label>Von *<input type="time" @bind="NewRule.StartTime" /></label>
                    <label>Bis *<input type="time" @bind="NewRule.EndTime" /></label>
                </div>
                <div class="form-row">
                    <label>Gültig von *<input type="date" @bind="NewRule.ValidFrom" /></label>
                    <label>bis *<input type="date" @bind="NewRule.ValidUntil" /></label>
                </div>
                <label>Farbe <input type="color" @bind="NewRule.Color" /></label>
                <label>
                    <InputCheckbox @bind-Value="NewRule.ExcludeSchoolHolidays" />
                    Schulferien ausschliessen
                </label>
                <label>Rhythmus
                    <InputSelect @bind-Value="NewRule.IntervalWeeks">
                        <option value="1">wöchentlich</option>
                        <option value="2">zweiwöchentlich</option>
                    </InputSelect>
                </label>
                <button type="submit">Regel erstellen + Slots generieren</button>
            </EditForm>
            break;

        case "Schulferien":
            <h2>Schulferien verwalten</h2>
            <p class="hint">Ferienzeiträume werden vom Serien-Generator automatisch
                ausgelassen (sofern "Schulferien ausschliessen" aktiviert).</p>
            <table class="admin-table">
                <thead><tr><th>Bezeichnung</th><th>Von</th><th>Bis</th><th></th></tr></thead>
                <tbody>
                    @foreach (var h in SchoolHolidays)
                    {
                        <tr>
                            <td>@h.Name</td>
                            <td>@h.HolidayFrom.ToString("dd.MM.yyyy")</td>
                            <td>@h.HolidayUntil.ToString("dd.MM.yyyy")</td>
                            <td><button @onclick="() => DeleteHolidayAsync(h.Id)">Löschen</button></td>
                        </tr>
                    }
                </tbody>
            </table>
            <EditForm Model="NewHoliday" OnValidSubmit="AddHolidayAsync">
                <label>Bezeichnung *<InputText @bind-Value="NewHoliday.Name" /></label>
                <div class="form-row">
                    <label>Von *<InputDate @bind-Value="NewHoliday.HolidayFrom" /></label>
                    <label>Bis *<InputDate @bind-Value="NewHoliday.HolidayUntil" /></label>
                </div>
                <button type="submit">Ferienperiode hinzufügen</button>
            </EditForm>
            break;
    }

    @if (ShowRejectDialog)
    {
        <div class="modal-overlay">
            <div class="modal-content">
                <h3>Buchung ablehnen</h3>
                <label>Ablehnungsgrund: <textarea @bind="RejectReason" rows="3"></textarea></label>
                <button @onclick="ConfirmRejectAsync" class="btn-reject">Ablehnen</button>
                <button @onclick="() => ShowRejectDialog = false">Abbrechen</button>
            </div>
        </div>
    }

    <!-- CSV-Export (bleibt HTTP-Download-Link) -->
    <div class="admin-export">
        <a href="/api/reservierung/admin/export/csv?von=@ExportFrom&bis=@ExportTo&nurBestaetigt=true"
           class="btn-secondary">CSV-Export für Buchhaltung</a>
    </div>
</div>

@code {
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
    private static readonly string[] Tabs =
        ["Pendente", "Alle Buchungen", "Serien-Regeln", "Schulferien"];

    private string ActiveTab = "Pendente";
    private IReadOnlyList<(BookingSlot Slot, HallMember? Member)> PendingSlots = [];
    private int PendingCount => PendingSlots.Count;
    private IReadOnlyList<RecurringRule> RecurringRules = [];
    private IReadOnlyList<SchoolHoliday> SchoolHolidays = [];
    private DateTime AdminWeek = DateTime.Today;
    private Dictionary<int, decimal> PriceBuffer = [];
    private CreateRecurringRuleCommand NewRule = new();
    private SchoolHolidayForm NewHoliday = new();
    private bool ShowRejectDialog;
    private int RejectingId;
    private string? RejectReason;
    private string ExportFrom = DateTime.Today.ToString("yyyy-MM-01");
    private string ExportTo   = DateTime.Today.ToString("yyyy-MM-dd");

    protected override async Task OnInitializedAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        PendingSlots    = await AdminSvc.GetPendingAsync();
        RecurringRules  = await RuleRepo.GetActiveRulesAsync();
        SchoolHolidays  = await AdminSvc.GetSchoolHolidaysAsync();
        PriceBuffer     = PendingSlots.ToDictionary(x => x.Slot.Id, x => x.Slot.PricePerBlock ?? 0);
    }

    private async Task ConfirmAsync(int id)
    {
        await ConfirmUseCase.ExecuteAsync(id, "admin");
        await LoadAllAsync();
    }

    private void OpenRejectDialog(int id) { RejectingId = id; ShowRejectDialog = true; }

    private async Task ConfirmRejectAsync()
    {
        await RejectUseCase.ExecuteAsync(RejectingId, RejectReason ?? "", "admin");
        ShowRejectDialog = false;
        await LoadAllAsync();
    }

    private async Task AdjustPriceAsync(int id)
    {
        if (PriceBuffer.TryGetValue(id, out var price))
            await AdminSvc.AdjustPriceAsync(id, price, null, "admin");
        await LoadAllAsync();
    }

    private async Task CreateRecurringRuleAsync()
    {
        await RecurringUseCase.ExecuteAsync(NewRule, "admin");
        NewRule = new();
        await LoadAllAsync();
    }

    private async Task DeactivateRuleAsync(int id)
    {
        await RuleRepo.DeactivateAsync(id);
        await LoadAllAsync();
    }

    private async Task AddHolidayAsync()
    {
        await AdminSvc.AddSchoolHolidayAsync(NewHoliday.Name, NewHoliday.HolidayFrom, NewHoliday.HolidayUntil);
        NewHoliday = new();
        await LoadAllAsync();
    }

    private async Task DeleteHolidayAsync(int id)
    {
        await AdminSvc.DeleteSchoolHolidayAsync(id);
        await LoadAllAsync();
    }

    private async Task LoadWeekSlotsAsync() { /* via GetWeekSlotsQuery */ }

    private record SchoolHolidayForm
    {
        public string Name = "";
        public DateOnly HolidayFrom = DateOnly.FromDateTime(DateTime.Today);
        public DateOnly HolidayUntil = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
    }
}
```

**Admin-Wrapper `Pages/ReservierungAdmin.cshtml`:**

```cshtml
@page
@model ReservierungAdminModel
@{ Layout = null; }
<!DOCTYPE html>
<html>
<head><title>Reservierungsverwaltung</title></head>
<body>
    <component type="typeof(ReservierungAdminComponent)" render-mode="Server" />
    <script src="/_blazor"></script>
</body>
</html>
```

```csharp
[UmbracoAdminAuthorize]
public class ReservierungAdminModel : PageModel
{
    public void OnGet() { }
}
```

### 5.2 Schulferien-Verwaltung

Ist als **Tab "Schulferien"** vollständig in `ReservierungAdminComponent.razor` integriert
(siehe 5.1). Kein separater Abschnitt oder eigene Razor Page notwendig.

Die REST-Endpunkte für Schulferien bleiben für direkten Zugriff erhalten:
```
GET    /api/reservierung/admin/schulferien
POST   /api/reservierung/admin/schulferien
DELETE /api/reservierung/admin/schulferien/{id}
```

Der Seriengenerator lädt `SchoolHolidays` einmalig pro Generierungslauf. Bestehende
Slots bei nachträglicher Ferienänderung werden vom Admin manuell gelöscht.

### 5.3 CSV-Export für die Buchhaltung

**Anforderung:** Alle Einzelbuchungen (nicht Serientermine, da separat abgerechnet) eines
Datumsbereichs mit Preisen, für Sammelrechnungen.

**Endpoint:** `GET /api/reservierung/admin/export/csv?von=2026-05-01&bis=2026-05-31&nurBestaetigt=true`

**Adapter:**

```csharp
public class BookingCsvAdapter : IBookingCsvPort
{
    private static readonly string[] Headers =
    [
        "Buchungs-ID", "Datum", "Von", "Bis", "Dauer (Min.)",
        "Mieter-Typ", "Ansprechperson", "Firma/Name",
        "Rechnungsadresse", "PLZ", "Ort", "E-Mail",
        "Blöcke", "CHF/Block", "Gesamtpreis", "Preisnotiz",
        "Status", "Angelegt am", "Notizen"
    ];

    public byte[] ExportBookings(IReadOnlyList<(BookingSlot Slot, HallMember? Member)> data,
                                 DateTime from, DateTime to)
    {
        var zurich = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(";", Headers.Select(Q)));

        foreach (var (slot, member) in data.Where(x => !x.Slot.IsRecurringSlot))
        {
            var start = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, zurich);
            var end   = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc,   zurich);
            var mins  = (int)(slot.Slot.EndUtc - slot.Slot.StartUtc).TotalMinutes;

            var cols = new List<string>
            {
                slot.Id.ToString(),
                start.ToString("dd.MM.yyyy"),
                start.ToString("HH:mm"),
                end.ToString("HH:mm"),
                mins.ToString(),
                member?.RenterType.Value.ToString() ?? "",
                member?.ContactPerson ?? "",
                member?.BillingName ?? "",
                member?.BillingAddress ?? "",
                member?.BillingPostalCode ?? "",
                member?.BillingCity ?? "",
                member?.Email ?? "",
                slot.TotalBlocks?.ToString() ?? "",
                slot.PricePerBlock?.ToString("F2") ?? "",
                slot.TotalPrice?.ToString("F2") ?? "",
                slot.PriceNote ?? "",
                slot.Status.ToString(),
                slot.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                slot.Notes ?? ""
            };

            sb.AppendLine(string.Join(";", cols.Select(Q)));
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    private static string Q(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
}
```

**Dateiname:** `Halle_Buchungen_{von:yyyy-MM}_{bis:yyyy-MM}.csv`

### 5.4 Audit-Log-Ansicht (Admin-Seite)

Eine einfache Tabelle am Ende der Admin-Seite:
`GET /api/reservierung/admin/audit?entityId={id}` liefert alle Einträge zum
gewählten `BookingSlot`. So ist jede Statusänderung, Preisanpassung und
Ablehnungsbegründung nachvollziehbar.

---

## 6. Implementierungsreihenfolge

1. **Umbraco Member Type `hallMember` anlegen** (via `ReservierungComposer`: `IMemberTypeService`, Custom Properties)
2. **Domain-Schicht** (`HallMember` Record, `BookingSlot`, `RecurringRule`, Value Objects, Ports inkl. `IMemberManagerPort`)
3. **`UmbracoMemberAdapter`** (implementiert `IMemberManagerPort` via `IMemberManager` + `SignInManager`)
4. **Infrastructure: Migration + Repositories** (4 Tabellen: `MagicLinkTokens`, `BookingSlots`, `RecurringRules`, `BookingAuditLog`, `SchoolHolidays`)
5. **Auth-Use-Cases** (`SendMagicLinkUseCase`, `ValidateMagicLinkUseCase`, `RegisterRenterUseCase`, `LoginWithPasswordUseCase`, `SetPasswordUseCase`, `RequestPasswordResetUseCase`, `ResetPasswordUseCase`)
6. **E-Mail-Adapter** (alle 7 Mail-Typen: Bestätigung, Admin, Magic Link, Registrierung+Link, Passwort-Reset, Ablehnung, Bestätigung)
7. **REST-Controller** (Auth-Endpunkte: Magic Link, Login mit Passwort, Passwort-Reset; Buchungs-API; Schulferien; CSV-Export)
8. **Buchungslogik** (`CreateBookingUseCase` mit Konfliktprüfung + Audit-Log)
9. **Seriengenerator** (`CreateRecurringRuleUseCase` mit `IntervalWeeks`, `ExcludeSchoolHolidays`)
10. **Umbraco-Konfig-Knoten** (uSync-Config, `IHallConfigurationPort`-Adapter)
11. **Blazor-Setup** (`Program.cs`: `AddServerSideBlazor()`, `MapBlazorHub()`, `_Imports.razor`)
12. **`WochenkalenderComponent.razor`** (Wochenansicht, Slot-Farbcodierung, Wochen-Navigation)
13. **`WochenkalenderComponent.razor` Mobile** (Tagansicht via `IsMobile`, Swipe-JS-Interop-Stub)
14. **`BuchungsPickerComponent.razor`** (4-Schritt-Flow: Dauer → Datum → Zeit → Bestätigen)
15. **Auth-Subflow im Picker** (Passwort-Login, Magic-Link-Fallback, Registrierungsformular, "Passwort einrichten"-Prompt nach Magic-Link-Login)
16. **Umbraco-Template-Shell** (`Reservierung.cshtml` mit `<component>`)
17. **`ReservierungAdminComponent.razor`** (Tabs: Pendente, Alle, Serien-Regeln, Schulferien)
18. **Admin-Wrapper-Page** (`ReservierungAdmin.cshtml` + `[UmbracoAdminAuthorize]`)
19. **CSV-Export** (Adapter + Download-Link im Admin-Tab)
20. **Audit-Log-Ansicht** als weiterer Admin-Tab
21. **Cross-Browser Mobile-Tests** (Swipe, Tagansicht, iOS-Safari, Android Chrome)
22. **Azure Deployment** (Env-Variablen, Blazor SignalR WebSocket prüfen)
23. **Umbraco Backoffice:** Reservierungs-Seite + Konfig-Knoten anlegen, erste Ferienperiode erfassen

---

## 7. Deployment-Checkliste

- [ ] `Program.cs`: `AddServerSideBlazor()` und `MapBlazorHub()` eintragen
- [ ] `_Imports.razor` anlegen (Blazor-weite Usings)
- [ ] Azure App Service Environment Variables: `Brevo__ApiKey`, `Turnstile__SiteKey`, `Turnstile__SecretKey`
- [ ] `appsettings.json`: Sektionen `Brevo` und `Turnstile` hinzufügen (Keys leer)
- [ ] DB-Migration läuft automatisch beim ersten App-Start (5 Tabellen: `MagicLinkTokens`, `BookingSlots`, `RecurringRules`, `BookingAuditLog`, `SchoolHolidays` — kein `HallRenters`/`HallSessions`/`PasswordResetTokens`)
- [ ] `ReservierungComposer` legt Member Type `hallMember` beim ersten Start an (oder via uSync/MemberTypes/hallMember.config)
- [ ] Umbraco Members: Lockout-Schwelle und Token-Ablauf in `IdentityOptions` konfigurieren
- [ ] uSync importiert `reservierung.config` und `reservierungKonfiguration.config` automatisch
- [ ] Umbraco Backoffice: Konfigurations-Knoten anlegen, Preis und Öffnungszeiten einstellen
- [ ] Umbraco Backoffice: Reservierungs-Seite anlegen, unter Homepage verschachteln, publizieren
- [ ] Cloudflare Turnstile: neue Site für `sporthalle-sulzerallee.ch` anlegen
- [ ] Blazor SignalR-Verbindung prüfen: Browser DevTools → Network → WS → `/_blazor` muss verbunden sein
- [ ] Azure App Service WebSockets aktivieren (Portal: App Service → Konfiguration → Allgemeine Einstellungen → Websockets: Ein)
- [ ] End-to-End Desktop: Registrierung (mit optionalem Passwort), Magic-Link aus Bestätigungsmail, Login mit Passwort, Passwort vergessen + Reset, Passwort nach Magic-Link-Login einrichten
- [ ] End-to-End Desktop: `WochenkalenderComponent` rendert Slots, Buchungs-Picker, Rückkehr mit `?session=confirmed`, Buchung abschliessen
- [ ] End-to-End Mobile (iOS Safari + Android Chrome): Tagansicht, Swipe-Navigation, Vollbild-Modal
- [ ] Admin: `ReservierungAdminComponent` alle 4 Tabs prüfen; Bestätigen/Ablehnen löst E-Mail aus; Preis inline anpassen; Serien-Regel erstellen; Ferienperiode erfassen
- [ ] Umbraco Backoffice Members-Tab: Mieterprofil sichtbar, Custom Properties korrekt befüllt
- [ ] Schulferien + Serienregel mit `ExcludeSchoolHolidays = true`: generierte Slots prüfen
- [ ] CSV-Export prüfen (Datumsbereich, Zeichensatz, Öffnung in Excel)
- [ ] Audit-Log verifizieren (Status-Übergänge, Preisänderung)
- [ ] Lasttest für Slot-Konfliktprüfung (2 simultane Buchungen auf gleichen Slot)
