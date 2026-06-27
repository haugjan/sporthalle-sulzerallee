# Sporthalle Sulzerallee

Website for Sporthalle Sulzerallee in Winterthur, Switzerland. Built with Umbraco 17 CMS on .NET 10.

Live site: `https://app-sporthalle-sulzerallee.azurewebsites.net/`

## Stack

| Layer | Technology |
|---|---|
| CMS | Umbraco 17.4.2 |
| Framework | .NET 10, ASP.NET Core |
| Local database | SQLite |
| Production database | Azure SQL |
| Media (production) | Azure Blob Storage |
| Email | Brevo REST API |
| CAPTCHA | Cloudflare Turnstile |
| Schema sync | uSync |
| CI/CD | GitHub Actions → Azure ZipDeploy |
| Hosting | Azure App Service Linux B1 |

## Language Convention

All code (types, namespaces, methods, identifiers) is written in English. Public-facing content shown to website visitors — UI labels, button text, page headings, email content, and URL paths — stays in German.

## Features

### Hall Booking (Reservierung)

Interactive calendar for booking time slots in the hall. Booking is guest-based (no renter login): the booking form captures contact and billing details and creates or updates an Umbraco Member record inline. The admin approves or rejects bookings, manages blockers and recurring appointments, and exports CSV reports.

**Public:** `/reservierung` — weekly calendar with available and booked time slots. Clicking a free slot opens a guest booking form protected by Cloudflare Turnstile.

**Admin:** Umbraco backoffice → "Booking" section. Tabs for pending requests (Anfragen), all bookings (Buchungen), blockers, recurring appointments (Serientermine), manual booking entry (Erfassen), and hall configuration (Konfiguration).

Code namespace: `SporthalleWeb.*.Booking`

### Passive Membership (Passivmitgliedschaft)

Supporters can symbolically adopt one square metre of the unihockey hall floor and become passive members at CHF 50, 100, or 200 per year.

**Public:** `/passivmitgliedschaft` — interactive SVG floor plan of the hall (300 fields, rendered as a Blazor Server component). Clicking a free field opens a 6-step registration wizard with Cloudflare Turnstile CAPTCHA.

**Admin:** Umbraco backoffice → "Passivmitglieder" section. Blazor Server admin UI with member table (sortable, mark as paid, notes), Excel export, and AbaNinja CSV export.

Code namespace: `SporthalleWeb.*.PassivMitgliedschaft` (pre-dates the English convention, retains German naming)

## Local Development

**Prerequisites:** .NET 10 SDK

```sh
cd src/SporthalleWeb
dotnet run
```

The app uses SQLite locally. On first boot, Umbraco runs all migrations and ContentSeeder creates 7 pages (takes 2-5 minutes). Subsequent starts are near-instant.

Local URL: `https://localhost:44343`
Backoffice login: `admin@localhost.dev` / `Admin1234!`

### Secrets (local)

The Brevo API key is NOT in git. Store it via:

```sh
cd src/SporthalleWeb
dotnet user-secrets set "Brevo:ApiKey" "<key>"
```

The Cloudflare Turnstile test keys (always-pass) are already in `appsettings.Development.json`.

## Repository Structure

The code is organised by **feature (vertical slices)** rather than technical
layers, with a shared top-level `Domain/` layer for the domain model. Each
feature's application logic, controllers, and components live under
`Features/{Feature}/`; adapters live under `Infrastructure/`.

```
src/SporthalleWeb/
  Domain/                   Domain model: aggregates, entities, value objects
    Booking/
      SlotAggregate/        BookingSlot, SlotType, TimeSlot, exceptions
      RecurringAggregate/   RecurringSlot
      HallMemberAggregate/  HallMember, RenterType, RenterEmail
    PassiveMembership/
      PassiveMemberAggregate/  PassiveMember, FieldNumber, MemberEmail, MembershipLevel, MemberStatus, VipField
  Features/                 Vertical slices (application + UI per feature)
    Booking/
      Ports/                Interfaces (no Port/Repository suffix)
      Calendar/             Week view, availability queries, public controller, calendar components
      Requests/             Create/Confirm/Reject booking, RegisterRenterCommand
      Admin/                Admin service, API + view controllers, admin components, manifest
      Recurring/            Create/Update/Delete recurring + admin component
      Configuration/        Admin config component (raw config via IHallConfigStore port)
      Dtos/                 API request/response records
    PassiveMembership/
      Registration/         Ports, RegisterMember, GetFieldStatuses, floor plan
      MemberAdmin/          PassiveMemberAdmin, admin API + view, admin components
  Infrastructure/           Adapters implementing feature ports (flat per feature)
    Booking/                Umbraco/Brevo/Turnstile/NPoco adapters, migration, composer
    PassiveMembership/      Umbraco/Brevo/Turnstile/ClosedXML adapters, migration, composer
    Shared/                 UmbracoDropdownHelper (shared by both features)
  ContentSeeder.cs          Seeds Umbraco pages on first boot
  MemberTypeSeeder.cs       Seeds hallMember + passivMember member types
  Views/                    Razor templates (Umbraco templates + MVC host views)
  uSync/                    Content type XML (committed, imported on startup)
  wwwroot/                  CSS, JS, media files
  Program.cs                App entry point
  appsettings.json          Base config (secrets empty)
  appsettings.Development.json  SQLite + dev overrides
```

## Deployment

Push to `main` triggers GitHub Actions:
1. `dotnet publish` with Release configuration
2. ZipDeploy to Azure App Service via Kudu API

`appsettings.Production.json` is injected from a GitHub Actions secret at deploy time and is never in git.

**Required Azure App Service environment variables:**

```
Brevo__ApiKey
Turnstile__SiteKey
Turnstile__SecretKey
ConnectionStrings__umbracoDbDSN        (Azure SQL connection string)
ConnectionStrings__umbracoDbDSN_ProviderName  (Microsoft.Data.SqlClient)
```

## Content Types

| Alias | Description |
|---|---|
| `homePage` | Root page (only one, auto-seeded) |
| `contentPage` | Standard content page with heading and body |
| `reservierung` | Hall booking page with calendar |
| `passivMitgliedschaft` | Passive membership page with floor plan |
| `reservierungKonfiguration` | Admin-only config node for booking settings |

Content type aliases are German (user-visible URLs). Content types are managed via uSync and imported automatically on startup.

## Language Policy

Code (namespaces, class names, method names, variable names, comments) is written in English. Public-facing content (UI text, page copy, emails) stays in German to match the target audience. A small number of identifiers remain in German to avoid breaking external contracts: URL routes, Umbraco content type aliases, the `PassivMitglieder` database table name, and the `App_Plugins/PassivMitglieder/` folder.

## Architecture Notes

The codebase is organised into **vertical slices** over a shared **`Domain/`**
layer. The domain model (aggregates, entities, value objects) lives in
`Domain/{Feature}/`; each feature's ports, application logic, controllers, and
Blazor components live together under `Features/{Feature}/`, instead of being
spread across technical layers. Inside a slice, the ports-and-adapters (hexagonal)
style still applies: the feature defines interfaces (ports), and
`Infrastructure/{Feature}/` provides the adapters. Features depend on `Domain`;
`Domain` depends on nothing.

Naming conventions inside a slice:

- **Ports** have no `Port`/`Repository` suffix (`IBookingSlots`, `IPassiveMembers`, `ICaptcha`).
- **Application classes** have no `UseCase`/`Query` suffix (`CreateBooking`, `GetWeekSlots`, `RegisterMember`); `Command` records are kept.
- **Adapters** carry a technology prefix (`UmbracoHallMembers`, `BrevoBookingEmail`, `TurnstileBookingCaptcha`, `ClosedXmlPassiveMemberExport`).

```
Domain/{Feature}/          Aggregates, entities, value objects (no dependencies)
Features/{Feature}/        Ports, application, controllers, components
Infrastructure/{Feature}/  Adapters implementing the feature's ports (flat)
Infrastructure/Shared/     Cross-feature helpers
```

Database access uses **NPoco** (Umbraco's built-in ORM) via `IScopeProvider` from `Umbraco.Cms.Infrastructure.Scoping`. Schema migrations run automatically on startup.

Hall renters are stored as **Umbraco Members** (member type `hallMember`), created or updated inline from the guest booking form. There is no renter login; member authentication was removed from the booking feature.

The layering is enforced by `SporthalleWeb.Tests/Architecture/LayerDependencyTests.cs`: a dependency-free source scan that fails if `Domain/` references an outer layer or framework, or if `Features/` references `Infrastructure` directly instead of going through a port.
