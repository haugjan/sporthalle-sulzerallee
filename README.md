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

Interactive calendar for booking time slots in the hall. Renters can register and book with Magic Link (passwordless) or password. The admin approves or rejects bookings, manages blockers and recurring appointments, and exports CSV reports.

**Public:** `/reservierung` — weekly calendar with available and booked time slots. Clicking a free slot opens a booking form (guest or logged-in).

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

```
src/SporthalleWeb/
  Application/       Business logic: use cases, queries, services
    Booking/
    PassivMitgliedschaft/
  Components/        Blazor admin components
    Booking/
  ContentSeeder.cs   Seeds Umbraco pages on first boot
  Domain/            Entities, value objects, ports
    Booking/
    PassivMitgliedschaft/
    Shared/
  Infrastructure/    Persistence, email, CAPTCHA adapters
    Booking/
    PassivMitgliedschaft/
    Shared/
  Presentation/      MVC controllers and DTOs
    Booking/
    PassivMitgliedschaft/
  Views/             Razor templates
  uSync/             Content type XML (committed, imported on startup)
  wwwroot/           CSS, JS, media files
  Program.cs         App entry point
  appsettings.json   Base config (secrets empty)
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

The codebase follows a hexagonal (ports and adapters) architecture for the two main features:

```
Domain (no dependencies)
  └── Ports (interfaces)
Application (depends on Domain)
  └── Use cases and queries
Infrastructure (depends on Domain)
  └── Adapters implementing ports
Presentation (depends on Application)
  └── Controllers and Razor views
```

Database access uses **NPoco** (Umbraco's built-in ORM) via `IScopeProvider` from `Umbraco.Cms.Infrastructure.Scoping`. Schema migrations run automatically on startup.

Hall renters are stored as **Umbraco Members** (member type `hallMember`) and authenticated via ASP.NET Core Identity with optional Magic Link (SHA-256 hashed, single-use, 20-minute TTL).
