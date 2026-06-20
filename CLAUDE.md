# Sporthalle Sulzerallee

## Project Overview

Website for the Sporthalle Sulzerallee in Winterthur. Built with **Umbraco 17.4.2** on **.NET 10**, deployed to **Azure App Service (Linux, B1 plan)**.

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

## Repository Layout

```
Sporthalle-Sulzerallee/
  src/
    SporthalleWeb/          # Main Umbraco project
      Views/                # Razor templates (Home.cshtml, ContentPage.cshtml) — committed
      uSync/                # Schema XML (content types, data types) — committed
      wwwroot/media/        # Static media files — committed (replaces Azure Blob for local dev)
      umbraco/Data/         # SQLite DB lives here locally — .gitignored (only .gitkeep committed)
      ContentSeeder.cs      # Startup seeder for content and templates
      Program.cs
      appsettings.json
      appsettings.Development.json  # SQLite connection string + uSync dev settings
      appsettings.Production.json   # NEVER in git — injected via Azure
  .github/workflows/
    deploy.yml              # GitHub Actions: dotnet publish → ZipDeploy to Azure
```

## Security Constraints

- `HMACSecretKey` must NEVER go into git
- `appsettings.Production.json` must NEVER go into git
- uSync **content** files must NOT go into git — only **schema** files (content types, data types) belong in the repo
- Azure subscription ID: `5a44bc29-c597-4d08-9ebf-212c359e3606` ("Sulzerallee Subscription") — switch back after Azure work

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
    "Settings": {
      "ImportOnStartup": true,
      "ExportOnSave": true
    }
  }
}
```

Backoffice login (local): `admin@localhost.dev` / `Admin1234!`

### HTTP vs HTTPS (local)

Umbraco's OpenIddict requires HTTPS by default. `Program.cs` disables this in Development:

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.PostConfigure<OpenIddictServerAspNetCoreOptions>(
        options => options.DisableTransportSecurityRequirement = true);
}
```

Run with `--urls http://localhost:PORT` to avoid certificate issues.

## Umbraco 17 API Notes

- `IContentService.SaveAndPublish()` does NOT exist in Umbraco 17.
- Correct publish pattern: `_contentService.Save(content, SuperUserId)` then `_contentService.Publish(content, new[] { "*" }, SuperUserId)`
- `IContent.TemplateId` is a nullable int; the DRAFT and PUBLISHED snapshots are independent.
- Saving a content type with a default template sets the draft node's TemplateId as a side effect but does NOT update the published NuCache snapshot. ContentSeeder accounts for this by calling `EnsureContentTemplates` BEFORE `EnsureContentTypeTemplates`.

## Deployment

GitHub Actions (`deploy.yml`) builds and ZipDeploys to Azure on push to `main`. The deploy uses Kudu's `/api/zipdeploy?async=true` with a 600s timeout (large zip due to media files ~119MB).

The `appsettings.Production.json` is injected at deploy time from a GitHub Actions secret (`APPSETTINGS_PRODUCTION`) and is NOT in git.

## Content Types (seeded by ContentSeeder)

| Alias                  | Name                  | Template               | Allowed under  |
|------------------------|-----------------------|------------------------|----------------|
| `homePage`             | Home Page             | `Home`                 | root           |
| `contentPage`          | Content Page          | `ContentPage`          | `homePage`     |
| `passivMitgliedschaft` | Passivmitgliedschaft  | `PassivMitgliedschaft` | `homePage`     |

## Pages (seeded)

Root: "Sporthalle Sulzerallee" (homePage)
Children: Unterstützung, Das Projekt, Über uns, Zweck, In den Medien, Kontakt

The "Passivmitgliedschaft" page must be created manually in the backoffice after first boot (Content → right-click root → Create → Passivmitgliedschaft).

## Passivmitgliedschaft Feature

Architecture follows a hexagonal pattern under `src/SporthalleWeb/`:

```
Domain/PassivMitgliedschaft/        # Entities, ports, value objects
Application/PassivMitgliedschaft/   # Use cases / queries
Infrastructure/PassivMitgliedschaft/ # Adapters: Brevo email, Turnstile CAPTCHA, EF Core
Presentation/PassivMitgliedschaft/  # Controllers, DTOs
```

Key external integrations:
- **Brevo**: transactional email (`Brevo:ApiKey` in appsettings)
- **Cloudflare Turnstile**: CAPTCHA (`Turnstile:SiteKey`, `Turnstile:SecretKey` in appsettings). Dev uses test key `1x00000000000000000000AA` (always passes) as fallback.

Frontend: `wwwroot/js/passivmitglied.js`, `wwwroot/css/passivmitglied.css`, `wwwroot/media/unihockey-boden.svg`

API endpoint: `POST /api/passivmitglieder/register`
