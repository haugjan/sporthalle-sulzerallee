# Sporthalle Sulzerallee

Umbraco CMS website for Sporthalle Sulzerallee.

## Stack

- **CMS:** Umbraco (ASP.NET Core)
- **Hosting:** Azure App Service B1, Switzerland North
- **Database:** Azure SQL Database Basic
- **Media Storage:** Azure Blob Storage
- **Content Sync:** uSync
- **CI/CD:** GitHub Actions

## Local Development

```bash
dotnet restore
dotnet run
```

Locally the site uses SQLite. Azure connection strings are configured as
App Service environment variables and are never stored in this repository.

## Content Synchronization

Content types and schema are managed via [uSync](https://jumoo.co.uk/usync/).
Exported definitions live in `uSync/`. Run an import after deployment to
apply schema changes.
