# Sporthalle Sulzerallee

## Project Overview

Website for Sporthalle Sulzerallee in Winterthur. Built with **Umbraco 17.4.2** on **.NET 10**, deployed to **Azure App Service (Linux, B1 plan)**.

Live site: `https://app-sporthalle-sulzerallee.azurewebsites.net/`

## Stack

- **CMS**: Umbraco 17.4.2
- **Framework**: .NET 10, ASP.NET Core
- **Local DB**: SQLite (via `Umbraco.Cms.Persistence.Sqlite`)
- **Production DB**: SQL Azure (connection string injected via Azure App Service environment variables)
- **Media (production)**: Azure Blob Storage (`Umbraco.StorageProviders.AzureBlob`)
- **Media (local dev)**: Static files under `src/SporthalleWeb/wwwroot/media/`
- **Schema sync**: uSync — exports content types and data types as XML to `src/SporthalleWeb/uSync/`
- **Content seeding**: `ContentSeeder.cs` — runs on `UmbracoApplicationStartedNotification`, seeds templates, content types, and pages on first boot
- **Email**: Brevo REST API (Template ID 1 for all transactional mails)
- **CAPTCHA**: Cloudflare Turnstile

## Language Convention

**All code is written in English.** This includes type names, namespaces, class names, method names, interface names, property names, and code comments.

**Public-facing content stays in German.** This covers: UI labels, button text, page headings, form field labels, email content, and any text visible to end users on the website. HTTP routes (e.g. `/reservierung`, `/passivmitgliedschaft`) and Umbraco content type aliases (e.g. `reservierung`, `passivMitgliedschaft`) also remain in German because they are user-visible URLs.

The Passivmitgliedschaft feature pre-dates this convention and still has German in its namespace (`SporthalleWeb.*.PassivMitgliedschaft`) and type names. These are not being migrated.

## Repository Layout

```
Sporthalle-Sulzerallee/
  src/
    SporthalleWeb/          # Main Umbraco project
      Application/          # Use cases, queries, service classes
<<<<<<< HEAD
        PassiveMembership/
        Reservierung/
      Components/           # Blazor components (admin UI + public)
        PassiveMembership/
        Reservierung/
      ContentSeeder.cs      # Startup seeder for content and templates
      Domain/               # Entities, value objects, ports
        PassiveMembership/
        Reservierung/
        Shared/
      Infrastructure/       # Adapters: email, CAPTCHA, persistence, members
        PassiveMembership/
        Reservierung/
        Shared/
      Pages/                # Razor Pages (legacy, mostly empty)
      Presentation/         # MVC Controllers, DTOs
        PassiveMembership/
        Reservierung/
=======
        Booking/
        PassivMitgliedschaft/
      Components/           # Blazor components (admin UI)
        Booking/
      ContentSeeder.cs      # Startup seeder for content and templates
      Domain/               # Entities, value objects, ports
        Booking/
        PassivMitgliedschaft/
        Shared/
      Infrastructure/       # Adapters: email, CAPTCHA, persistence, members
        Booking/
        PassivMitgliedschaft/
        Shared/
      Pages/                # Razor Pages (legacy, mostly empty)
      Presentation/         # MVC Controllers, DTOs
        Booking/
        PassivMitgliedschaft/
>>>>>>> worktree-feature-reservierung
      Program.cs
      appsettings.json
      appsettings.Development.json  # SQLite + uSync dev settings
      appsettings.Production.json   # NEVER in git — injected via Azure
      Views/                # Razor templates
      uSync/                # Schema XML (content types, data types) — committed
      wwwroot/media/        # Static media files — committed
      umbraco/Data/         # SQLite DB lives here locally — .gitignored
  .github/workflows/
    deploy.yml              # GitHub Actions: dotnet publish → ZipDeploy to Azure
  CLAUDE.md                 # This file
  README.md
```

## Security Constraints

- `HMACSecretKey` must NEVER go into git
- `appsettings.Production.json` must NEVER go into git
- uSync **content** files must NOT go into git — only **schema** files (content types, data types) belong in the repo
- Azure subscription ID: `5a44bc29-c597-4d08-9ebf-212c359e3606` ("Sulzerallee Subscription") — switch back after Azure work
- Brevo API key stored via `dotnet user-secrets` only, NEVER in git (UserSecretsId: `a2f4c8e1-3b7d-4f92-8a5e-6c1d9b0e2f47`)

## Local Development

### First run (fresh checkout)

```sh
cd src/SporthalleWeb
dotnet run
```

**Important:** always run `dotnet run` from `src/SporthalleWeb/`, not from the repo root. The SQLite connection string is a relative path (`umbraco/Data/Umbraco.sqlite.db`) resolved against the content root; `Program.cs` rewrites it to an absolute path, but running from the right directory avoids confusion.

On first boot, Umbraco runs all schema migrations, then `ContentSeeder` seeds 7 pages. This takes 2-5 minutes. The app listens at `https://localhost:44343`.

### Subsequent runs

`ContentSeeder` detects existing root content and skips seeding (near-instant).

### Config for local dev (`appsettings.Development.json`)

```json
{
  "Umbraco": {
    "CMS": {
      "Hosting": { "Debug": true },
      "Unattended": {
        "InstallUnattended": true,
        "UnattendedUserName": "Admin",
        "UnattendedUserEmail": "admin@localhost.dev",
        "UnattendedUserPassword": "Admin1234!"
      }
    }
  },
  "ConnectionStrings": {
    "umbracoDbDSN": "Data Source=umbraco/Data/Umbraco.sqlite.db;Foreign Keys=True;Pooling=True",
    "umbracoDbDSN_ProviderName": "Microsoft.Data.Sqlite"
  },
  "uSync": {
    "Settings": { "ImportOnStartup": true, "ExportOnSave": true }
  },
  "Turnstile": {
    "SiteKey": "1x00000000000000000000AA",
    "SecretKey": "1x0000000000000000000000000000000AA"
  }
}
```

Backoffice login (local): `admin@localhost.dev` / `Admin1234!`

### Local secrets

The Brevo API key is stored via `dotnet user-secrets` and is shared across all git worktrees:

```sh
cd src/SporthalleWeb
dotnet user-secrets set "Brevo:ApiKey" "<key>"
```

Secrets are stored at `%APPDATA%\Microsoft\UserSecrets\a2f4c8e1-3b7d-4f92-8a5e-6c1d9b0e2f47\secrets.json` and loaded automatically in Development mode.

### HTTP vs HTTPS (local)

Umbraco's OpenIddict requires HTTPS by default. `Program.cs` disables this in Development:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddictServerAspNetCoreOptions>(
        options => options.DisableTransportSecurityRequirement = true);
}
```

## Deployment

GitHub Actions (`deploy.yml`) builds and ZipDeploys to Azure on push to `main`. The deploy uses Kudu's `/api/zipdeploy?async=true` with a 600 s timeout (large zip due to media files ~119 MB).

`appsettings.Production.json` is injected at deploy time from a GitHub Actions secret (`APPSETTINGS_PRODUCTION`) and is NOT in git.

## Umbraco 17 API — Critical Notes

- `IContentService.SaveAndPublish()` does NOT exist in Umbraco 17.
- Correct publish pattern:
  ```csharp
  _contentService.Save(content, SuperUserId);
  _contentService.Publish(content, new[] { "*" }, SuperUserId);
  ```
- `IContent.TemplateId` is a nullable int; draft and published NuCache snapshots are independent. ContentSeeder calls `EnsureContentTemplates` BEFORE `EnsureContentTypeTemplates` because of this.
- Use `IScopeProvider` from `Umbraco.Cms.Infrastructure.Scoping` (NOT `Umbraco.Cms.Core.Scoping`). The Core namespace `ICoreScope` has no `.Database` property.
- NPoco async methods take `Sql` objects:
  ```csharp
  // Correct:
  var count = await scope.Database.ExecuteScalarAsync<int>(
      new Sql("SELECT COUNT(*) FROM T WHERE X = @0", value));
  // Wrong — no such overload in Umbraco 17:
  await scope.Database.ExecuteScalarAsync<int>("SELECT ... WHERE X = @0", value);
  ```
- `PrimaryKeyAttribute` in NPoco uses property syntax:
  ```csharp
  [PrimaryKey("Id", AutoIncrement = true)]  // correct
  [PrimaryKey("Id", autoIncrement: true)]   // CS1739 — wrong
  ```
- Culture: `Program.cs` sets `de-CH` as the default culture for all threads so date and number formatting is consistent with Swiss locale across both production (Linux) and local dev (Windows).

## Content Types

| Alias | Name | Template | Allowed under |
|---|---|---|---|
| `homePage` | Home Page | `Home` | root only |
| `contentPage` | Content Page | `ContentPage` | `homePage` |
| `reservierung` | Reservierung | `Reservierung` | `homePage` |
| `passivMitgliedschaft` | Passivmitgliedschaft | `PassivMitgliedschaft` | `homePage` |
| `reservierungKonfiguration` | Reservierung Konfiguration | (none) | root only |

Note: content type aliases and template names are German (user-visible URLs). The code that handles them is English.

### Content Type Properties

**contentPage**: `pageHeading` (TextBox), `bodyContent` (TextArea), `pageImage` (TextBox)

**reservierung**: `pageHeading` (TextBox), `pageImage` (TextBox), `introText` (TextArea), `textAfter` (TextArea)

**passivMitgliedschaft**: `pageHeading` (TextBox), `pageImage` (TextBox), `introText` (RichText), `outroText` (RichText)

**reservierungKonfiguration**: `pricePerBlock`, `blockDurationMinutes`, `openingHourStart`, `openingHourEnd`, `buchbareDauern`, `anlaesse`, `preisText`

## Pages Seeded on First Boot

Root node: "Sporthalle Sulzerallee" (homePage)
Children seeded as contentPage: Unterstützung, Das Projekt, Über uns, Zweck, In den Medien, Kontakt

**Manual steps after first boot:**
1. Create "Reservierung" page: Content → right-click root → Create → Reservierung → publish
2. Create "Passivmitgliedschaft" page: Content → right-click root → Create → Passivmitgliedschaft → publish
3. Create "Reservierung Konfiguration" node at content root (allowed at root; no public template)

---

## Feature: Passive Membership (Passivmitgliedschaft)

Allows supporters to symbolically adopt one square metre of the unihockey hall floor and become passive members.

Note: this feature pre-dates the English convention and retains German in its namespace and type names.

### Architecture

```
Domain/PassiveMembership/
  PassiveMember.cs               Aggregate Root
  FieldNumber.cs                 Value Object (1–300)
  MemberEmail.cs                 Value Object (normalised lowercase)
  MembershipLevel.cs             Value Object (Bronze / Silber / Gold)
  VipField.cs                    Named areas (Torraum, Anspielkreis, Anspielpunkt)
  DomainException.cs             + FieldAlreadyTakenException + MemberNotFoundException
  Events/MemberRegisteredEvent.cs
  Ports/IPassiveMemberRepository.cs
  Ports/IEmailPort.cs
  Ports/IExcelPort.cs
  Ports/IAbaninjaCsvPort.cs
  Ports/ICaptchaPort.cs

Application/PassiveMembership/
  RegisterMemberCommand.cs
  RegisterMemberUseCase.cs       Checks field free → saves → sends email
  GetFieldStatusesQuery.cs       Returns occupied fields with VIP labels
  FieldStatusDto.cs
  AdminService.cs                MarkAsPaid, UpdateNotes, Excel/CSV export

Infrastructure/PassiveMembership/
  Composition/PassiveMemberComposer.cs      IComposer, DI registration
  Email/BrevoEmailOptions.cs
  Email/BrevoEmailAdapter.cs                Brevo REST API, Template ID 1
  Excel/ClosedXmlExcelAdapter.cs            Excel export (ClosedXML NuGet)
  Excel/AbaninjaCsvAdapter.cs               AbaNinja import CSV
  Captcha/TurnstileOptions.cs
  Captcha/TurnstileCaptchaAdapter.cs        Cloudflare Turnstile
  Persistence/PassiveMemberDbRecord.cs      NPoco POCO (table: PassivMitglieder)
  Persistence/PassiveMemberRepository.cs    IScopeProvider from Infrastructure.Scoping
  Persistence/PassiveMemberMigration.cs     v1 + v2

Presentation/PassiveMembership/
  Controllers/PassiveMemberController.cs        REST API (public)
  Controllers/PassiveMemberAdminController.cs   Backoffice admin (single iframe host)
  Controllers/FloorPlanController.cs            Public floor plan page (iframe)
  Dtos/RegisterMemberRequest.cs
  Dtos/FieldStatusResponse.cs

Components/PassiveMembership/
  PmAdminComponent.razor         Main admin shell: subnav + tab routing
  PmMembersComponent.razor       Member table (sortable, mark as paid, notes)
  PmExportsComponent.razor       Excel + AbaNinja CSV download buttons
  FloorPlanComponent.razor       Interactive SVG floor plan + 6-step registration wizard

App_Plugins/PassivMitglieder/    Umbraco backoffice section registration
  pm-entrypoint.js               Custom Element <pm-admin>: renders single iframe to /passivmitglieder/admin

Views/PassivMitgliedschaft.cshtml            Umbraco template shell (filename matches template alias)
Views/PassiveMemberAdmin/Index.cshtml        Blazor host for PmAdminComponent
Views/FloorPlan/Index.cshtml                 Blazor host for FloorPlanComponent

wwwroot/css/passivmitglied.css
wwwroot/js/passivmitglied.js
wwwroot/media/unihockey-boden.svg
```

### Admin UI

The Umbraco backoffice section loads `pm-entrypoint.js`, which renders a single `<iframe src="/passivmitglieder/admin">`. Inside that iframe, `PmAdminComponent` (Blazor Server) provides the subnav and routes between two child components:

- **Mitglieder tab** → `PmMembersComponent`: sortable member table, mark-as-paid button, inline notes
- **Exporte tab** → `PmExportsComponent`: Excel and AbaNinja CSV export buttons

Auth: Umbraco backoffice session cookie (inherited by the iframe).

### Public Floor Plan

The `_PassivMitgliedschaftModule.cshtml` partial embeds an `<iframe src="/passivmitglieder/hallenboden">`, which loads `FloorPlanComponent` (Blazor Server). The component renders a 20×15 SVG grid (300 fields) and a 6-step registration wizard with Cloudflare Turnstile CAPTCHA.

### Membership Levels

| Key | Display Name (German, public-facing) | Annual Fee |
|---|---|---|
| `Bronze` | Hallenbodenbesitzer | CHF 50 |
| `Silber` | Chnebler | CHF 100 |
| `Gold` | Cüpli-Chnebler | CHF 200 |

### Floor Plan VIP Areas (German labels, public-facing)

| Area | Fields |
|---|---|
| Torraum | Left and right goal creases |
| Anspielkreis | Centre circle |
| Anspielpunkte | Face-off spots |

### Database Table: `PassivMitglieder`

Table name kept in German to avoid a destructive migration. All column names are English.

| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY | PK |
| FieldNumber | INT UNIQUE | 1–300 |
| FirstName, LastName | NVARCHAR(100) | |
| AddressLine | NVARCHAR(300) | |
| PostalCode, City | NVARCHAR | |
| Country | NVARCHAR(100) | Default 'Schweiz' |
| Email | NVARCHAR(200) | |
| MembershipLevel | NVARCHAR(20) | 'Bronze'/'Silber'/'Gold' |
| ShowNameOnFloor | BIT | |
| DisplayName | NVARCHAR(200) NULL | |
| CreatedAt | DATETIME | |
| PaidAt | DATETIME NULL | |
| Notes | NVARCHAR(MAX) NULL | |

Migration: `PassiveMemberMigration` plan (v1: create table, v2: drop+recreate for correct IDENTITY). Runs automatically via `PassiveMemberComposer`.

### REST API

```
GET  /api/passivmitglieder/felder
     → { occupiedFields: [{fieldNumber, displayName, vipLabel}], totalFields: 300 }

POST /api/passivmitglieder/register
     Body: { fieldNumber, firstName, lastName, addressLine, postalCode, city,
             email, levelKey, showNameOnFloor, displayName, captchaToken }
     → 200 | 409 Conflict (field taken) | 400 Bad Request

Admin (Umbraco backoffice auth):
GET  /api/passivmitglieder/admin/members
POST /api/passivmitglieder/admin/{id}/paid
POST /api/passivmitglieder/admin/{id}/notes
GET  /api/passivmitglieder/admin/export/excel
GET  /api/passivmitglieder/admin/export/abaninja
```

Routes use the German URL segment `/passivmitglieder/` intentionally — it is part of the public URL structure.

### Email

All emails via Brevo REST API, Template ID 1.
Admin BCC: `bettina.zahnd@`, `matthias.lehner@`, `jan.haug@` (all `@sporthalle-sulzerallee.ch`)
Brevo API key: `Brevo:ApiKey` config — `dotnet user-secrets` locally, Azure App Service env var in production.

---

## Feature: Booking (Reservierung)

Allows hall renters to book time slots via an interactive weekly calendar. Supports guest booking and account-based booking with Magic Link or password auth.

All code is under the `Booking` namespace (`SporthalleWeb.*.Booking`). The public-facing URL path and Umbraco content type alias remain `reservierung`.

### Architecture

```
Domain/Booking/
  BookingSlot.cs               Aggregate Root
  SlotType.cs                  Enum: Blocker/Reserved/Booked/Rejected/Recurring
  HallMember.cs                Lightweight record wrapping Umbraco Member
  MagicLinkToken.cs
  RecurringSlot.cs             Recurring appointment series
  TimeSlot.cs                  Value Object (UTC start/end)
  RenterEmail.cs               Value Object
  RenterType.cs                Enum: Privatperson/Verein/Firma/Behörde
  DomainException.cs
  SlotConflictException.cs
  Ports/IMemberManagerPort.cs
  Ports/IBookingSlotRepository.cs
  Ports/IMagicLinkTokenRepository.cs
  Ports/IBookingAuditRepository.cs
  Ports/IBookingEmailPort.cs
  Ports/IBookingCsvPort.cs
  Ports/IHallConfigurationPort.cs
  Ports/IRecurringSlotRepository.cs

Domain/Shared/
  ICaptchaPort.cs

Application/Booking/
  GetWeekSlotsQuery.cs
  GetAvailableDaysQuery.cs
  GetAvailableTimeSlotsQuery.cs
  GetRecurringSlotsQuery.cs      Returns RecurringSlotWithYearCount list
  SlotOption.cs
  WeekSlotDto.cs
  CreateBookingUseCase.cs
  CreateBookingCommand.cs
  ConfirmBookingUseCase.cs
  RejectBookingUseCase.cs
  CreateRecurringSlotUseCase.cs  Creates recurring slot series; uses RecurringSlotCommand
  UpdateRecurringSlotUseCase.cs
  DeleteRecurringSlotUseCase.cs
  SendMagicLinkUseCase.cs        SHA-256 hashed tokens, 20 min TTL
  ValidateMagicLinkUseCase.cs
  RegisterRenterUseCase.cs
  RegisterRenterCommand.cs
  LoginWithPasswordUseCase.cs
  SetPasswordUseCase.cs
  RequestPasswordResetUseCase.cs
  ResetPasswordUseCase.cs
  BookingAdminService.cs
  HallConfigService.cs           Key-value store for booking configuration

Infrastructure/Booking/
  Composition/BookingComposer.cs
  Config/UmbracoHallConfigurationAdapter.cs  IHallConfigurationPort
  Members/UmbracoMemberAdapter.cs            IMemberManagerPort via IMemberManager + SignInManager
  Email/BrevoBookingEmailAdapter.cs
  Export/BookingCsvAdapter.cs
  Persistence/
    BookingSlotRepository.cs
    MagicLinkTokenRepository.cs
    BookingAuditRepository.cs
    RecurringSlotRepository.cs
    BookingMigration.cs         BookingMigrationPlan v1.0.0 → v1.1.0 → v1.2.0 → v1.3.0
    DbRecords/

Infrastructure/Shared/
  TurnstileCaptchaAdapter.cs

Presentation/Booking/
  Controllers/BookingController.cs              REST API (public)
  Controllers/BookingAuthController.cs          Auth pages (MVC)
  Controllers/BookingAdminController.cs         Backoffice admin view
  Controllers/BookingAdminApiController.cs      Admin REST API
  Controllers/BookingBackofficeAdminController.cs
  BookingManifestComposer.cs                    Backoffice section (alias: Sporthalle.Booking)
  Dtos/

Components/Booking/
  BookingAdminComponent.razor       Root admin shell (tab navigation + badge)
  AdminRequestsComponent.razor      Pending bookings (tab: Anfragen)
  AdminBookingsComponent.razor      All bookings (tab: Buchungen)
  AdminBlockerComponent.razor       Blockers (tab: Blocker)
  AdminRecurringComponent.razor     Recurring slots (tab: Serientermine)
  AdminCreateComponent.razor        Admin calendar booking entry (tab: Erfassen)
  AdminConfigurationComponent.razor HallConfig editor (tab: Konfiguration)
  AdminEditDialogComponent.razor    Shared edit dialog with day-calendar time picker
  WeeklyCalendarComponent.razor     Public-facing weekly calendar
  BookingPickerComponent.razor      Time slot picker

Views/Reservierung.cshtml                     Public calendar page (template name: Reservierung)
Views/BookingAdmin/Index.cshtml               Admin dashboard shell
Views/BookingAuth/Index.cshtml                Auth redirect page
Views/BookingAuth/Anmelden.cshtml             Login page
Views/BookingAuth/Registrieren.cshtml         Registration page
Views/BookingBackofficeAdmin/Index.cshtml     Backoffice admin view
Views/Partials/_BookingCalendar.cshtml        Booking calendar partial
Views/Shared/_BackofficeLayout.cshtml         Admin backoffice layout

wwwroot/css/reservierung.css
wwwroot/js/reservierung.js        Public calendar
wwwroot/js/reservierung-admin.js  Admin backoffice
```

### Slot Types

| SlotType | Meaning |
|---|---|
| `Blocker` | Admin-blocked, not bookable, shown as striped anthracite |
| `Reserved` | Pending admin confirmation, shown in azure blue with diagonal stripes |
| `Booked` | Confirmed booking |
| `Rejected` | Rejected (soft delete), hidden in public calendar |
| `Recurring` | Recurring appointment occurrence, public, blocks external bookings |

The `Type` column in `BookingSlots` stores the enum name as a string (e.g. `"Recurring"`).

### Database Tables

**BookingSlots**

| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY | PK |
| MemberId | INT NULL | Umbraco IMember.Id |
| Type | NVARCHAR(20) | Blocker/Reserved/Booked/Rejected/Recurring |
| StartUtc | DATETIME2 | |
| EndUtc | DATETIME2 | |
| Title | NVARCHAR(300) | Event name |
| Color | NVARCHAR(7) NULL | Hex color |
| Notes | NVARCHAR(MAX) NULL | |
| CreatedAt, UpdatedAt | DATETIME2 | |
| CreatedBy | NVARCHAR(200) | |

**MagicLinkTokens**

| Column | Type | Notes |
|---|---|---|
| Id | INT IDENTITY | PK |
| MemberId | INT | Umbraco IMember.Id |
| TokenHash | NVARCHAR(128) UNIQUE | SHA-256 of plaintext token |
| ExpiresAt | DATETIME2 | 20 min from creation |
| UsedAt | DATETIME2 NULL | |
| CreatedAt | DATETIME2 | |
| RemoteIp | NVARCHAR(45) NULL | |

**BookingAuditLog**: append-only log of all state changes.

**HallConfig**: key-value store for booking settings. Keys: `openingHourStart`, `openingHourEnd`, `blockDurationMinutes`, `pricePerBlock`, `buchbareDauern`, `anlaesse`, `preisText`, `bookingCutoffDate`.

Note: the HallConfig keys `buchbareDauern`, `anlaesse`, `preisText` are stored DB strings (not code identifiers); they pre-date the English convention and are not changed to avoid a data migration. The C# methods accessing them are English: `GetBookableDurationsAsync`, `GetEventTypesAsync`, `GetPreisTextAsync`.

Migration plan: `BookingMigrationPlan` versions v1.0.0–v1.3.0. Runs via `BookingMigrationComponent`.

### Renter Accounts (Hall Members)

Stored as **Umbraco Members** with member type alias `hallMember`.

Custom member properties: `renterType`, `billingName`, `billingAddress`, `billingPostalCode`, `billingCity`, `billingCountry`, `phone`, `hasKey`, `magicLinkSentAt`, `passwordResetSentAt`

`UmbracoMemberAdapter` wraps `IMemberManager` + `SignInManager<MemberIdentityUser>`.
Admin view of renters: Umbraco Backoffice → Members.

### Authentication

**Magic Link (primary method):**
1. `POST /api/reservierung/auth/magic-link` with `{ email }`
2. `SendMagicLinkUseCase`: generates 64 random bytes (Base64Url), stores SHA-256 hash in `MagicLinkTokens`, emails plaintext link
3. User clicks link: `GET /reservierung/auth/validate?token=...`
4. `ValidateMagicLinkUseCase`: hashes token, finds record, checks expiry/used, marks used, calls `SignInAsync`
5. Redirect to `/reservierung?session=confirmed`

Rate limit: 1 magic link per 10 min per email.

**Password (optional):** Set after first magic link login. BCrypt via ASP.NET Core Identity.
Password reset uses Identity TOTP tokens (no custom token table).

**Guest booking:** `POST /api/reservierung/gast-buchung` — no session required; creates/updates member inline. The route string is German (user-visible URL); the C# method is `GuestBooking`.

### REST API

```
Public:
GET  /api/reservierung/konfiguration
GET  /api/reservierung/wochen-slots?von=YYYY-MM-DD
GET  /api/reservierung/verfuegbare-tage?monat=YYYY-MM&dauern=60
GET  /api/reservierung/verfuegbare-slots?datum=YYYY-MM-DD&dauern=60
POST /api/reservierung/gast-buchung
POST /api/reservierung/auth/magic-link
POST /api/reservierung/auth/validate
POST /api/reservierung/auth/register
POST /api/reservierung/auth/login
POST /api/reservierung/auth/request-password-reset
POST /api/reservierung/auth/reset-password

Authenticated renter:
POST /api/reservierung/auth/logout
POST /api/reservierung/auth/password
GET  /api/reservierung/me
GET  /api/reservierung/meine-buchungen
POST /api/reservierung/buchungen

Admin (Umbraco admin role):
GET  /api/admin/reservierungen/pending
POST /api/admin/reservierungen/{id}/confirm
POST /api/admin/reservierungen/{id}/reject
DELETE /api/admin/reservierungen/{id}
GET  /api/admin/reservierungen/export?von=YYYY-MM-DD&bis=YYYY-MM-DD
```

Note: HTTP routes are German (user-visible URLs). Controller class names and action method names are English.

### Auth Pages (MVC views)

```
GET  /reservierung                         Public calendar
GET  /reservierung/auth/validate?token=    Magic link validation + redirect
GET  /reservierung/anmelden                Login page
POST /reservierung/anmelden/password
POST /reservierung/anmelden/magic-link
GET  /reservierung/registrieren            Registration page
POST /reservierung/registrieren
```

### Admin Backoffice

URL: `/reservierung/admin/` (Umbraco section alias `Sporthalle.Booking`, weight 101, appears after Content)
Tabs: Anfragen (pending Reserved), Buchungen (all), Blocker (all Blockers), Serientermine (recurring slots), Erfassen (admin booking with full calendar UI), Konfiguration (HallConfig editor)

Tab labels are German (public-facing). The Blazor component names behind them are English:

| Tab label | Component |
|---|---|
| Anfragen | `AdminRequestsComponent` |
| Buchungen | `AdminBookingsComponent` |
| Blocker | `AdminBlockerComponent` |
| Serientermine | `AdminRecurringComponent` |
| Erfassen | `AdminCreateComponent` |
| Konfiguration | `AdminConfigurationComponent` |

The shared edit dialog for Buchungen and Blocker tabs is `AdminEditDialogComponent` — it exposes a public `OpenAsync(int id)` method and is referenced via Blazor `@ref`.

### Email

Brevo REST API, Template ID 1. Admin BCC: `jan.haug@sporthalle-sulzerallee.ch`

Mail types: provisional confirmation, admin notification, booking confirmed, booking rejected with reason, magic link, registration confirmation with first link, password reset.

Emails are styled with Sporthalle header, color bar, and footer.

---

## NuGet Packages (key)

| Package | Purpose |
|---|---|
| `Umbraco.Cms` | Core CMS |
| `Umbraco.Cms.Persistence.Sqlite` | SQLite for local dev |
| `Umbraco.StorageProviders.AzureBlob` | Azure Blob for production media |
| `uSync` | Content type / schema sync |
| `ClosedXML` | Excel export (Passivmitgliedschaft) |
| `Microsoft.ICU.ICU4C.Runtime` | Consistent globalization across platforms |

---

## NPoco / Database Access Patterns

Always use `IScopeProvider` from `Umbraco.Cms.Infrastructure.Scoping`:

```csharp
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

public class MyRepository(IScopeProvider scopeProvider)
{
    public async Task<int> CountAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var result = await scope.Database.ExecuteScalarAsync<int>(
            new Sql("SELECT COUNT(*) FROM MyTable WHERE Active = @0", true));
        scope.Complete();
        return result;
    }
}
```

NPoco POCO:
```csharp
[TableName("MyTable")]
[PrimaryKey("Id", AutoIncrement = true)]
public class MyRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

Migration:
```csharp
public class MyMigrationPlan : MigrationPlan
{
    public MyMigrationPlan() : base("MyPlan")
    {
        From(string.Empty).To<MyMigration>("my-v1");
    }
}

public class MyMigration(IMigrationContext context) : MigrationBase(context)
{
    protected override void Migrate()
    {
        if (!TableExists("MyTable"))
            Create.Table<MyRecord>().Do();
    }
}
```

---

## Blazor Setup

Blazor Server is active globally for admin components:

- `Program.cs`: `builder.Services.AddServerSideBlazor()` and `app.MapBlazorHub()`
- `_Imports.razor`: global `@using` statements
- `_Layout.cshtml`: `<script src="_framework/blazor.server.js"></script>`

<<<<<<< HEAD
Admin and public components are embedded in Razor views (with `Layout = null`) via:
```cshtml
<component type="typeof(PmAdminComponent)" render-mode="Server" />
```

PassiveMembership components live in `Components/PassiveMembership/`, Reservierung components in `Components/Reservierung/`.
=======
Admin components in `Components/Booking/` are embedded in admin Razor views via:
```cshtml
<component type="typeof(AdminConfigurationComponent)" render-mode="Server" />
```

The `BookingAdminComponent` is the root shell for the backoffice tab navigation. It renders one of the six sub-components based on the active tab.
>>>>>>> worktree-feature-reservierung

---

## Git Worktrees

The `.claude/worktrees/` directory contains git worktrees for feature branches:
- `feature-passivmitgliedschaft` → branch `worktree-feature-passivmitgliedschaft`
- `feature-reservierung` → branch `worktree-feature-reservierung`

After merging both branches into main and syncing main back, all three branches are kept at the same HEAD.
