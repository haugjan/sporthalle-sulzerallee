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

The project is organised by **feature (vertical slices)**, with a shared
top-level **`Domain/`** layer holding the domain model (aggregates, entities,
value objects). Each feature's application code, controllers, and components live
under `Features/{Feature}/`; adapters and cross-cutting helpers live under
`Infrastructure/`. (See "Architecture: Vertical Slicing".)

```
Sporthalle-Sulzerallee/
  src/
    SporthalleWeb/          # Main Umbraco project
      Domain/               # Domain model: aggregates, entities, value objects
        Booking/            # namespace SporthalleWeb.Domain.Booking
          SlotAggregate/        # BookingSlot, SlotType, TimeSlot, DomainException, SlotConflictException
          RecurringAggregate/   # RecurringSlot
          HallMemberAggregate/  # HallMember, RenterType, RenterEmail
        PassiveMembership/
          PassiveMemberAggregate/  # PassiveMember, FieldNumber, MemberEmail, MembershipLevel, MemberStatus, VipField, DomainException
                                   # namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate
      Features/             # Vertical slices — application/controllers/components per feature
        Booking/            # namespace SporthalleWeb.Features.Booking (single, flat)
          Ports/                # Interfaces (no Port/Repository suffix)
          Calendar/             # GetWeekSlots, GetAvailableDays/TimeSlots, BookingController, calendar components
          Requests/             # CreateBooking (+Command), ConfirmBooking, RejectBooking, RegisterRenterCommand
          Admin/                # BookingAdminService, API/view/backoffice controllers, admin components, BookingManifestComposer
          Recurring/            # Create/Update/Delete recurring, GetRecurringSlots, AdminRecurringComponent
          Configuration/        # AdminConfigurationComponent (raw config via IHallConfigStore port)
          Dtos/                 # API request/response records
        PassiveMembership/  # sub-namespaces per slice (Registration, MemberAdmin)
          Registration/         # ports, RegisterMember, GetFieldStatuses, FloorPlan controller+component+view
          MemberAdmin/          # PassiveMemberAdmin, admin API + view controllers, Pm* components, views
      Infrastructure/       # Adapters implementing feature ports (flat per feature)
        Booking/            # Umbraco/Brevo/Turnstile/NPoco adapters, repositories, migration, composer, aliases
        PassiveMembership/  # Umbraco/Brevo/Turnstile/ClosedXML adapters, repository, migration, composer, aliases
        Shared/             # UmbracoDropdownHelper (used by both features)
      ContentSeeder.cs      # Startup seeder for content and templates
      MemberTypeSeeder.cs   # Seeds hallMember + passivMember member types
      Pages/                # Razor Pages (legacy, mostly empty)
      Program.cs
      appsettings.json
      appsettings.Development.json  # SQLite + uSync dev settings
      appsettings.Production.json   # NEVER in git — injected via Azure
      Views/                # Razor templates (Umbraco templates + MVC host views)
      uSync/                # Schema XML (content types, data types) — committed
      wwwroot/media/        # Static media files — committed
      umbraco/Data/         # SQLite DB lives here locally — .gitignored
  .github/workflows/
    deploy.yml              # GitHub Actions: dotnet publish → ZipDeploy to Azure
  CLAUDE.md                 # This file
  README.md
```

## Architecture: Vertical Slicing

Code is grouped by feature, not by technical layer. The domain model (aggregates,
entities, value objects) lives in a shared top-level `Domain/{Feature}/` layer;
the feature's application logic, controllers, ports, and components live under
`Features/{Feature}/`. There is no top-level `Application/`, `Presentation/`, or
`Components/` folder. Inside a slice the ports-and-adapters style still holds:
the feature declares interfaces (ports), and `Infrastructure/{Feature}/` supplies
the adapters. Features depend on `Domain`; `Domain` depends on nothing.

Naming rules inside a slice:

- **Ports**: no `Port`/`Repository` suffix — `IBookingSlots`, `IRecurringSlots`, `IHallMembers`, `IHallConfiguration`, `ICaptcha`, `IPassiveMembers`, `IPassiveMemberEmail`.
- **Application classes**: no `UseCase`/`Query`/`Service`/`Manager` suffix — `CreateBooking`, `GetWeekSlots`, `RegisterRenter`, `RegisterMember`, `GetFieldStatuses`. `Command` records are kept (`CreateBookingCommand`). Exceptions kept for historical reasons: `BookingAdminService`, `PassiveMemberAdmin`.
- **Adapters**: technology prefix, no `Adapter` suffix — `UmbracoHallMembers`, `UmbracoHallConfiguration`, `BrevoBookingEmail`, `BookingCsvExport`, `TurnstileBookingCaptcha`, `UmbracoPassiveMembers`, `BrevoPassiveMemberEmail`, `TurnstilePassiveCaptcha`, `ClosedXmlPassiveMemberExport`, `AbaninjaPassiveMemberExport`.

Namespaces:
- **Domain**: `SporthalleWeb.Domain.Booking` (flat) and `SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate`.
- **Booking feature**: single flat namespace `SporthalleWeb.Features.Booking` for the whole slice (components set it with `@namespace`; `Features/Booking/_Imports.razor` adds `@using SporthalleWeb.Domain.Booking`).
- **PassiveMembership feature**: per-slice sub-namespaces (`SporthalleWeb.Features.PassiveMembership.Registration`, `.MemberAdmin`).
- **Infrastructure**: `SporthalleWeb.Infrastructure.{Feature}` (flat) and `SporthalleWeb.Infrastructure.Shared`.

HTTP routes, view contents, Umbraco aliases, and runtime behaviour are unchanged by the slicing; only code organisation and type names changed. Umbraco templates (e.g. `Views/Reservierung.cshtml`, `Views/PassivMitgliedschaft.cshtml`) stay in `Views/` because Umbraco resolves templates from there.

The layering is enforced (not just by convention) by `SporthalleWeb.Tests/Architecture/LayerDependencyTests.cs`: a dependency-free source scan that fails the suite if `Domain/` references an outer layer or framework (Infrastructure, Features, Umbraco, NPoco) or if `Features/` references `SporthalleWeb.Infrastructure` directly instead of going through a Port.

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

Note: code (namespace `SporthalleWeb.Features.PassiveMembership.*`, type names) is English. German remains only in user-visible/contract identifiers: the content type alias `passivMitgliedschaft`, the `App_Plugins/PassivMitglieder/` folder, the `passivMember` member type alias, and HTTP routes.

### Architecture

```
Domain/PassiveMembership/PassiveMemberAggregate/   ns SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate
  PassiveMember.cs               Aggregate Root
  FieldNumber.cs                 Value Object (1–300)
  MemberEmail.cs                 Value Object (normalised lowercase)
  MembershipLevel.cs             Value Object (Bronze / Silber / Gold)
  MemberStatus.cs                Pending / Confirmed / Deleted
  VipField.cs                    Named areas (Torraum, Anspielkreis, Anspielpunkt)
  DomainException.cs             + FieldAlreadyTakenException + MemberNotFoundException

Features/PassiveMembership/
  Registration/                                   ns ...Features.PassiveMembership.Registration
    IPassiveMembers.cs               (was IPassiveMemberRepository)
    IPassiveMemberEmail.cs           (was IEmailPort)
    ICaptcha.cs                      (was ICaptchaPort)
    RegisterMember.cs                (was RegisterMemberUseCase + RegisterMemberCommand)
    GetFieldStatuses.cs              (was GetFieldStatusesQuery + FieldStatusDto)
    RegisterMemberRequest.cs, FieldStatusResponse.cs   API DTOs
    PassiveMemberController.cs       REST API (public: felder, register)
    FloorPlanController.cs           Public floor plan page (iframe)
    FloorPlanComponent.razor         Interactive SVG floor plan + 6-step wizard
    Views/FloorPlan.cshtml           Blazor host for FloorPlanComponent
  MemberAdmin/                                     ns ...Features.PassiveMembership.MemberAdmin
    PassiveMemberAdmin.cs            (was AdminService) MarkAsPaid, UpdateNotes, exports
    IPassiveMemberExport.cs          (was IExcelPort)
    IPassiveMemberAbaninja.cs        (was IAbaninjaCsvPort)
    PassiveMemberAdminController.cs       REST admin API
    PassiveMemberAdminViewController.cs   Backoffice iframe host
    PmAdminComponent.razor           Admin shell: subnav + tab routing
    PmMembersComponent.razor         Member table (sortable, mark as paid, notes)
    PmExportsComponent.razor         Excel + AbaNinja CSV download buttons
    PmRequestsComponent.razor        Pending requests
    Views/Admin.cshtml               Blazor host for PmAdminComponent

Infrastructure/PassiveMembership/    (flat, ns SporthalleWeb.Infrastructure.PassiveMembership)
  PassiveMemberComposer.cs          IComposer, DI registration
  PassiveMemberManifestReader.cs    Backoffice section manifest
  PassivMemberAliases.cs            Member property aliases (single source of truth)
  UmbracoPassiveMembers.cs          (was PassiveMemberRepository) Umbraco Members storage
  BrevoPassiveMemberEmail.cs / BrevoEmailOptions.cs   Brevo REST API, Template ID 1
  ClosedXmlPassiveMemberExport.cs   Excel export (ClosedXML)
  AbaninjaPassiveMemberExport.cs    AbaNinja import CSV
  TurnstilePassiveCaptcha.cs / TurnstileOptions.cs    Cloudflare Turnstile
  PassiveMemberMigration.cs         migration plan (v9 drops legacy table)

App_Plugins/PassivMitglieder/    Umbraco backoffice section registration
  pm-entrypoint.js               Custom Element <pm-admin>: renders single iframe to /passivmitglieder/admin

Views/PassivMitgliedschaft.cshtml  Umbraco template shell (filename matches template alias — stays in Views/)

wwwroot/css/passivmitglied.css
wwwroot/js/passivmitglied.js
wwwroot/media/unihockey-boden.svg
```

Passive members are stored as **Umbraco Members** (member type `passivMember`); there is no longer a dedicated `PassivMitglieder` table (dropped in migration v9).

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

Allows hall renters to book time slots via an interactive weekly calendar. Booking is guest-based: there is no renter login. A booking captures the renter's contact and billing details and creates or updates an Umbraco Member record inline. (Member authentication, Magic Link, and password login were removed; see commit "Remove member authentication from booking feature".)

All Booking feature code shares one flat namespace `SporthalleWeb.Features.Booking` (components set it via `@namespace`). The public-facing URL path and Umbraco content type alias remain `reservierung`.

### Architecture

The domain model lives in `Domain/Booking/` (namespace `SporthalleWeb.Domain.Booking`).
All `Features/Booking/` files share namespace `SporthalleWeb.Features.Booking` (folders
are organisation only) and reference the domain via `using SporthalleWeb.Domain.Booking`
(added in `Features/Booking/_Imports.razor` for components). Ports lose the
`Port`/`Repository` suffix; application classes lose `UseCase`/`Query`; adapters get a
technology prefix.

```
Domain/Booking/         namespace SporthalleWeb.Domain.Booking
  SlotAggregate/        BookingSlot (Aggregate Root), SlotType, TimeSlot, DomainException, SlotConflictException
  RecurringAggregate/   RecurringSlot
  HallMemberAggregate/  HallMember, RenterEmail, RenterType (Privatperson/Verein/Firma/Schule)

Features/Booking/
  Ports/                IBookingSlots, IRecurringSlots, IBookingAudit,
                        IBookingEmail, IBookingCsv, IHallConfiguration, IHallConfigStore, IHallMembers, ICaptcha
                        (port files are named exactly after the interface they declare, e.g. IHallMembers.cs)
  Calendar/             GetWeekSlots, GetAvailableDays, GetAvailableTimeSlots, SlotOption, WeekSlotDto,
                        BookingController (public REST), WeeklyCalendarComponent, BookingPickerComponent,
                        DateInputComponent, TimePickerComponent
  Requests/             CreateBooking (+CreateBookingCommand), ConfirmBooking, RejectBooking,
                        RegisterRenterCommand (member detail carrier used when a booking creates/updates a member)
  Admin/                BookingAdminService, BookingAdminApiController, BookingAdminController,
                        BookingBackofficeAdminController, BookingManifestComposer (section alias Sporthalle.Booking),
                        BookingAdminComponent (shell), AdminRequestsComponent (Anfragen), AdminBookingsComponent (Buchungen),
                        AdminBlockerComponent (Blocker), AdminCreateComponent (Erfassen), AdminEditDialogComponent
  Recurring/            CreateRecurringSlot (+RecurringSlotCommand), UpdateRecurringSlot, DeleteRecurringSlot,
                        GetRecurringSlots, AdminRecurringComponent (Serientermine)
  Configuration/        AdminConfigurationComponent (Konfiguration); raw key-value config is the IHallConfigStore port (adapter UmbracoHallConfigStore)
  Dtos/                 BookingSlotDto, AdminBookingResponse, HallMemberDto, CreateBookingRequest

Infrastructure/Booking/    (flat, ns SporthalleWeb.Infrastructure.Booking)
  BookingComposer.cs                  IComposer, DI registration
  UmbracoHallConfiguration.cs         IHallConfiguration (typed, domain-shaped config reader)
  UmbracoHallConfigStore.cs           IHallConfigStore (raw key-value access to the HallConfig table; owns all HallConfig SQL)
  UmbracoHallMembers.cs               IHallMembers via IMemberManager + IMemberService
  BrevoBookingEmail.cs                IBookingEmail (Brevo REST API)
  BookingCsvExport.cs                 IBookingCsv
  TurnstileBookingCaptcha.cs          ICaptcha (Cloudflare Turnstile)
  BookingSlotRepository.cs, BookingAuditRepository.cs, RecurringSlotRepository.cs
  *Record.cs                          NPoco POCOs (BookingSlot, BookingAuditLog, HallConfig, RecurringSlot)
  HallMemberAliases.cs                Member property aliases
  BookingMigration.cs                 BookingMigrationPlan v1.0.0 → v1.10.0

Infrastructure/Shared/
  UmbracoDropdownHelper.cs            shared by Booking + PassiveMembership

Views/Reservierung.cshtml                     Umbraco template (alias Reservierung — stays in Views/)
Views/BookingAdmin/Index.cshtml               Admin dashboard shell
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

(The `MagicLinkTokens` table was dropped with the auth removal; migration `DropMagicLinkTokensV11`, v1.10.0.)

**BookingAuditLog**: append-only log of all state changes.

**HallConfig**: key-value store for booking settings. Keys: `openingHourStart`, `openingHourEnd`, `blockDurationMinutes`, `pricePerBlock`, `buchbareDauern`, `anlaesse`, `preisText`, `bookingCutoffDate`.

Note: the HallConfig keys `buchbareDauern`, `anlaesse`, `preisText` are stored DB strings (not code identifiers); they pre-date the English convention and are not changed to avoid a data migration. The C# methods accessing them are English: `GetBookableDurationsAsync`, `GetEventTypesAsync`, `GetPreisTextAsync`.

Migration plan: `BookingMigrationPlan` versions v1.0.0–v1.10.0. Runs via `BookingMigrationComponent`.

### Renter Accounts (Hall Members)

Stored as **Umbraco Members** with member type alias `hallMember`.

Custom member properties: `renterType`, `orgName`, `contactFirstName`, `contactLastName`, `billingAddress`, `addressLine2`, `billingPostalCode`, `billingCity`, `billingCountry`, `phone`, `hasKey`, `notes`, `color`. The single source of truth is `Infrastructure/Booking/HallMemberAliases.cs` (referenced by both `MemberTypeSeeder` and `UmbracoHallMembers`). `color` is the renter's preferred calendar colour (Umbraco ColorPicker, hex value).

`UmbracoHallMembers` wraps `IMemberManager` + `IMemberService`.
Admin view of renters: Umbraco Backoffice → Members.

### Booking flow (guest only)

There is no renter login. A booking is always a guest booking:

`POST /api/reservierung/gast-buchung` — no session required. It validates the CAPTCHA, then creates or updates the `hallMember` member record inline from the submitted contact and billing details and stores the slot as `Reserved` pending admin confirmation. The route string is German (user-visible URL); the C# action method is `GuestBooking`.

### REST API

```
All routes live on BookingController, prefix `api/reservierung`.

Public:
GET  /api/reservierung/konfiguration
GET  /api/reservierung/wochen-slots?von=YYYY-MM-DD
GET  /api/reservierung/verfuegbare-tage?monat=YYYY-MM&dauern=60
GET  /api/reservierung/verfuegbare-slots?datum=YYYY-MM-DD&dauern=60
POST /api/reservierung/gast-buchung

Admin (Umbraco admin role):
GET    /api/reservierung/admin/pending
POST   /api/reservierung/admin/buchungen/{id}/confirm
POST   /api/reservierung/admin/buchungen/{id}/reject
DELETE /api/reservierung/admin/buchungen/{id}
GET    /api/reservierung/admin/export?von=YYYY-MM-DD&bis=YYYY-MM-DD
```

Note: HTTP routes are German (user-visible URLs). Controller class names and action method names are English.

### MVC Pages

```
GET  /reservierung   Public calendar (Umbraco template Reservierung.cshtml)
```

The login, registration, and magic-link validation pages were removed with member authentication.

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

Mail types: provisional confirmation, admin notification, booking confirmed, booking rejected with reason.

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
- `_Imports.razor` (root) + `Features/_Imports.razor`: global `@using` statements for components
- `_Layout.cshtml`: `<script src="_framework/blazor.server.js"></script>`

Components are embedded in Razor views (with `Layout = null`) via:
```cshtml
<component type="typeof(PmAdminComponent)" render-mode="Server" />
```

Components live inside their feature slice under `Features/{Feature}/` (e.g.
`Features/PassiveMembership/MemberAdmin/PmAdminComponent.razor`,
`Features/Booking/Admin/AdminBookingsComponent.razor`). Booking components set
`@namespace SporthalleWeb.Features.Booking` so the whole slice shares one namespace.
Shared components (e.g. `ConfirmDialogComponent`) remain in `Components/Shared/`.

The `BookingAdminComponent` is the root shell for the backoffice tab navigation. It renders one of the six sub-components based on the active tab.

---

## Git Worktrees

The `.claude/worktrees/` directory contains git worktrees for feature branches:
- `feature-passivmitgliedschaft` → branch `worktree-feature-passivmitgliedschaft`
- `feature-reservierung` → branch `worktree-feature-reservierung`

After merging both branches into main and syncing main back, all three branches are kept at the same HEAD.
