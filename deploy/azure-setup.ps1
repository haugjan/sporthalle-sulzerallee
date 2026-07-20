#Requires -Version 7
#
# One-time Azure provisioning for the Sporthalle Sulzerallee dev environment.
#
# Production resources already exist (not recreated by this script):
#   plan-sporthalle (B3, switzerlandnorth) — prod App Service Plan
#   app-sporthalle-sulzerallee             — prod Web App
#   sql-sporthalle / db-sporthalle         — prod SQL Server + Database
#   sporthallesulzerallee                  — prod Storage Account (media)
#
# This script creates:
#   asp-sporthalle-dev (B1)               — dev App Service Plan (small, cheap)
#   app-sporthalle-sulzerallee-dev        — dev Web App
#   db-sporthalle-dev (Basic)             — dev SQL Database (same server as prod)
#   stsporthalledev                       — dev Storage Account (media)
#   sporthalle-github-deploy              — Entra App Registration for OIDC (dev + prod)
#
# Disables SCM basic auth on both apps and assigns Website Contributor to the
# app registration on both apps. After running this script, remove the KUDU_USER
# and KUDU_PASS secrets from GitHub — they are no longer used.
#
# Run:
#   az login
#   pwsh deploy/azure-setup.ps1

$ErrorActionPreference = 'Stop'

# ── Configuration ─────────────────────────────────────────────────────────────
$Subscription   = '5a44bc29-c597-4d08-9ebf-212c359e3606'
$ResourceGroup  = 'Sporthalle-Sulzerallee'
$Location       = 'switzerlandnorth'

$DevPlan        = 'asp-sporthalle-dev'
$DevApp         = 'app-sporthalle-sulzerallee-dev'
$DevPlanSku     = 'B1'
$DevSqlDb       = 'db-sporthalle-dev'
$DevStorage     = 'stsporthalledev'    # globally unique, max 24 lowercase alphanumeric

$ProdApp        = 'app-sporthalle-sulzerallee'
$SqlServer      = 'sql-sporthalle'
$SqlAdmin       = 'sporthalle-admin'
$MediaContainer = 'media'

$GhRepo         = 'haugjan/sporthalle-sulzerallee'
$AppRegName     = 'sporthalle-github-deploy'
# ─────────────────────────────────────────────────────────────────────────────

# SQL admin password (env var or interactive prompt)
$SqlPass = $env:SQL_PASS
if (-not $SqlPass) {
    $SqlPass = Read-Host "SQL admin password for '$SqlAdmin'" -AsSecureString |
        ConvertFrom-SecureString -AsPlainText
}
if (-not $SqlPass) { throw 'SQL admin password is required.' }

# Dev Umbraco backoffice admin password for the unattended first-boot install
$DevUmbracoPass = $env:DEV_UMBRACO_PASS
if (-not $DevUmbracoPass) {
    $DevUmbracoPass = Read-Host 'Dev backoffice admin password (min 10 chars)' -AsSecureString |
        ConvertFrom-SecureString -AsPlainText
}
if (-not $DevUmbracoPass) { throw 'Dev Umbraco admin password is required.' }

Write-Host '==> Selecting subscription'
az account set --subscription $Subscription

# ── Dev App Service Plan ──────────────────────────────────────────────────────
Write-Host "==> Dev App Service plan ($DevPlan, Linux, $DevPlanSku)"
az appservice plan create `
    --resource-group $ResourceGroup --name $DevPlan `
    --location $Location --is-linux --sku $DevPlanSku

# ── Dev Web App ───────────────────────────────────────────────────────────────
Write-Host "==> Dev Web App ($DevApp, .NET 10)"
az webapp create `
    --resource-group $ResourceGroup --plan $DevPlan --name $DevApp `
    --runtime 'DOTNETCORE:10.0'

az webapp config set `
    --resource-group $ResourceGroup --name $DevApp `
    --always-on true --output none

# ── Dev SQL Database ──────────────────────────────────────────────────────────
Write-Host "==> Dev SQL Database ($DevSqlDb on $SqlServer, Basic tier)"
az sql db create `
    --resource-group $ResourceGroup --server $SqlServer `
    --name $DevSqlDb --service-objective Basic

$DevConn = "Server=tcp:${SqlServer}.database.windows.net,1433;" +
    "Initial Catalog=${DevSqlDb};User ID=${SqlAdmin};Password=${SqlPass};" +
    'Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

# ── Dev Storage Account ───────────────────────────────────────────────────────
Write-Host "==> Dev Storage Account ($DevStorage)"
az storage account create `
    --resource-group $ResourceGroup --name $DevStorage `
    --location $Location --sku Standard_LRS --kind StorageV2 `
    --allow-blob-public-access true --min-tls-version TLS1_2

$DevStorageConn = az storage account show-connection-string `
    --resource-group $ResourceGroup --name $DevStorage `
    --query connectionString -o tsv

az storage container create `
    --name $MediaContainer --public-access blob `
    --connection-string $DevStorageConn --output none

# ── Dev App Settings ──────────────────────────────────────────────────────────
Write-Host '==> Dev App Settings (SQL, blob, unattended install, test Turnstile keys)'
az webapp config appsettings set `
    --resource-group $ResourceGroup --name $DevApp `
    --settings `
        "ConnectionStrings__umbracoDbDSN=$DevConn" `
        'ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.SqlClient' `
        "Umbraco__Storage__AzureBlob__Media__ConnectionString=$DevStorageConn" `
        "Umbraco__Storage__AzureBlob__Media__ContainerName=$MediaContainer" `
        'Umbraco__CMS__Unattended__InstallUnattended=true' `
        'Umbraco__CMS__Unattended__UpgradeUnattended=true' `
        'Umbraco__CMS__Unattended__UnattendedUserName=Admin' `
        'Umbraco__CMS__Unattended__UnattendedUserEmail=admin@dev.sporthalle-sulzerallee.ch' `
        "Umbraco__CMS__Unattended__UnattendedUserPassword=$DevUmbracoPass" `
        'uSync__Settings__ExportOnSave=false' `
        'Turnstile__SiteKey=1x00000000000000000000AA' `
        'Turnstile__SecretKey=1x0000000000000000000000000000000AA' `
    --output none

# ── Disable SCM basic auth on both apps ──────────────────────────────────────
foreach ($AppName in @($DevApp, $ProdApp)) {
    Write-Host "==> Disable SCM basic auth on $AppName"
    az resource update `
        --resource-group $ResourceGroup --namespace Microsoft.Web `
        --parent "sites/$AppName" --resource-type basicPublishingCredentialsPolicies `
        --name scm --set properties.allow=false --output none
}

# ── Entra App Registration ────────────────────────────────────────────────────
Write-Host "==> Entra App Registration ($AppRegName)"
$AppId = az ad app create --display-name $AppRegName --query appId -o tsv

try { az ad sp create --id $AppId --output none 2>$null } catch {}
$SpObjectId = az ad sp show --id $AppId --query id -o tsv

foreach ($Env in @('dev', 'prod')) {
    Write-Host "==> Federated credential: GitHub Environment '$Env'"
    $CredJson = @{
        name      = "github-sporthalle-$Env"
        issuer    = 'https://token.actions.githubusercontent.com'
        subject   = "repo:${GhRepo}:environment:$Env"
        audiences = @('api://AzureADTokenExchange')
    } | ConvertTo-Json
    $CredFile = [System.IO.Path]::GetTempFileName() + '.json'
    $CredJson | Set-Content $CredFile -Encoding utf8
    az ad app federated-credential create --id $AppId --parameters "@$CredFile"
    Remove-Item $CredFile
}

# ── Website Contributor role on both apps ─────────────────────────────────────
foreach ($AppName in @($DevApp, $ProdApp)) {
    Write-Host "==> Role assignment: Website Contributor on $AppName"
    $AppScope  = az webapp show -g $ResourceGroup -n $AppName --query id -o tsv
    $RaGuid    = [System.Guid]::NewGuid().ToString()
    $RoleDefId = "/subscriptions/$Subscription/providers/Microsoft.Authorization/roleDefinitions/de139f84-1756-47ae-9be6-808fbbe84772"
    $RoleJson = @{
        properties = @{
            roleDefinitionId = $RoleDefId
            principalId      = $SpObjectId
            principalType    = 'ServicePrincipal'
        }
    } | ConvertTo-Json
    $RoleFile = [System.IO.Path]::GetTempFileName() + '.json'
    $RoleJson | Set-Content $RoleFile -Encoding utf8
    try {
        az rest --method put `
            --url "https://management.azure.com${AppScope}/providers/Microsoft.Authorization/roleAssignments/${RaGuid}?api-version=2022-04-01" `
            --headers 'Content-Type=application/json' `
            --body "@$RoleFile" `
            --output none
    } catch {
        Write-Host '  (role assignment may already exist; continuing)'
    }
    Remove-Item $RoleFile
}

$TenantId = az account show --query tenantId -o tsv

Write-Host @"

Done. GitHub configuration (https://github.com/$GhRepo/settings):

  Repository secrets  (Settings -> Secrets and variables -> Actions -> Repository secrets):
    AZURE_CLIENT_ID       = $AppId
    AZURE_TENANT_ID       = $TenantId
    AZURE_SUBSCRIPTION_ID = $Subscription

  Create two GitHub Environments (Settings -> Environments), then add per-env variable:
    'dev'  -> Variables -> AZURE_WEBAPP_NAME = $DevApp
    'prod' -> Variables -> AZURE_WEBAPP_NAME = $ProdApp

  Set via GitHub CLI:
    gh secret set AZURE_CLIENT_ID       --repo $GhRepo --body '$AppId'
    gh secret set AZURE_TENANT_ID       --repo $GhRepo --body '$TenantId'
    gh secret set AZURE_SUBSCRIPTION_ID --repo $GhRepo --body '$Subscription'
    gh variable set AZURE_WEBAPP_NAME   --repo $GhRepo --env dev  --body '$DevApp'
    gh variable set AZURE_WEBAPP_NAME   --repo $GhRepo --env prod --body '$ProdApp'

  Once KUDU_USER / KUDU_PASS are no longer needed, delete them from GitHub.

Remaining manual steps:
  1. Set Brevo API key on both apps:
       az webapp config appsettings set -g $ResourceGroup -n $DevApp  --settings "Brevo__ApiKey=<key>"
       az webapp config appsettings set -g $ResourceGroup -n $ProdApp --settings "Brevo__ApiKey=<key>"
  2. Set real Turnstile keys on the prod app:
       az webapp config appsettings set -g $ResourceGroup -n $ProdApp --settings "Turnstile__SiteKey=<key>" "Turnstile__SecretKey=<key>"
  3. Disable ExportOnSave on the prod app:
       az webapp config appsettings set -g $ResourceGroup -n $ProdApp --settings "uSync__Settings__ExportOnSave=false"
  4. Push to a feature/* branch to trigger the first dev deploy.
     The dev DB starts empty -- Umbraco installs itself unattended on first request (~2-3 min).
"@
