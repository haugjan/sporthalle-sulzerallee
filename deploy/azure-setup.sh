#!/usr/bin/env bash
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
# Disables SCM basic auth on both apps and issues Website Contributor role to the
# app registration on both apps. After this script you can remove the KUDU_USER and
# KUDU_PASS secrets from GitHub — they are no longer used.
#
# Run after logging in:
#     az login
#     bash deploy/azure-setup.sh
#
set -euo pipefail

# ── Configuration ─────────────────────────────────────────────────────────────
SUBSCRIPTION="5a44bc29-c597-4d08-9ebf-212c359e3606"
RESOURCE_GROUP="Sporthalle-Sulzerallee"
LOCATION="switzerlandnorth"

DEV_PLAN="asp-sporthalle-dev"
DEV_APP="app-sporthalle-sulzerallee-dev"
DEV_PLAN_SKU="B1"
DEV_SQL_DB="db-sporthalle-dev"
DEV_STORAGE="stsporthalledev"   # globally unique, max 24 lowercase alphanumeric chars

PROD_APP="app-sporthalle-sulzerallee"
SQL_SERVER="sql-sporthalle"
SQL_ADMIN="sporthalle-admin"
MEDIA_CONTAINER="media"

GH_REPO="haugjan/sporthalle-sulzerallee"
APP_REG_NAME="sporthalle-github-deploy"
# ─────────────────────────────────────────────────────────────────────────────

# SQL admin password: env var or interactive prompt
: "${SQL_PASS:=}"
if [ -z "$SQL_PASS" ]; then read -rs -p "SQL admin password for '$SQL_ADMIN': " SQL_PASS || true; echo; fi
if [ -z "$SQL_PASS" ]; then
  echo "ERROR: SQL admin password not provided." >&2; exit 1
fi

# Umbraco dev backoffice admin password (for the unattended first-boot install)
: "${DEV_UMBRACO_PASS:=}"
if [ -z "$DEV_UMBRACO_PASS" ]; then read -rs -p "Dev backoffice admin password (min 10 chars): " DEV_UMBRACO_PASS || true; echo; fi
if [ -z "$DEV_UMBRACO_PASS" ]; then
  echo "ERROR: Dev Umbraco admin password not provided." >&2; exit 1
fi

echo "==> Selecting subscription"
az account set --subscription "$SUBSCRIPTION"

# ── Dev App Service Plan ──────────────────────────────────────────────────────
echo "==> Dev App Service plan ($DEV_PLAN, Linux, $DEV_PLAN_SKU)"
az appservice plan create \
  --resource-group "$RESOURCE_GROUP" --name "$DEV_PLAN" \
  --location "$LOCATION" --is-linux --sku "$DEV_PLAN_SKU"

# ── Dev Web App ───────────────────────────────────────────────────────────────
echo "==> Dev Web App ($DEV_APP, .NET 10)"
az webapp create \
  --resource-group "$RESOURCE_GROUP" --plan "$DEV_PLAN" --name "$DEV_APP" \
  --runtime "DOTNETCORE:10.0"

az webapp config set \
  --resource-group "$RESOURCE_GROUP" --name "$DEV_APP" --always-on true --output none

# ── Dev SQL Database ──────────────────────────────────────────────────────────
echo "==> Dev SQL Database ($DEV_SQL_DB on $SQL_SERVER, Basic tier)"
az sql db create \
  --resource-group "$RESOURCE_GROUP" --server "$SQL_SERVER" \
  --name "$DEV_SQL_DB" --service-objective "Basic"

DEV_CONN="Server=tcp:${SQL_SERVER}.database.windows.net,1433;Initial Catalog=${DEV_SQL_DB};User ID=${SQL_ADMIN};Password=${SQL_PASS};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# ── Dev Storage Account ───────────────────────────────────────────────────────
echo "==> Dev Storage Account ($DEV_STORAGE, for Umbraco media)"
az storage account create \
  --resource-group "$RESOURCE_GROUP" --name "$DEV_STORAGE" \
  --location "$LOCATION" --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access true --min-tls-version TLS1_2

DEV_STORAGE_CONN="$(az storage account show-connection-string \
  --resource-group "$RESOURCE_GROUP" --name "$DEV_STORAGE" --query connectionString -o tsv)"

az storage container create \
  --name "$MEDIA_CONTAINER" --public-access blob \
  --connection-string "$DEV_STORAGE_CONN" --output none

# ── Dev App Settings ──────────────────────────────────────────────────────────
echo "==> Dev App Settings (SQL, blob, unattended install, test Turnstile keys)"
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" --name "$DEV_APP" \
  --settings \
    "ConnectionStrings__umbracoDbDSN=${DEV_CONN}" \
    "ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.SqlClient" \
    "Umbraco__Storage__AzureBlob__Media__ConnectionString=${DEV_STORAGE_CONN}" \
    "Umbraco__Storage__AzureBlob__Media__ContainerName=${MEDIA_CONTAINER}" \
    "Umbraco__CMS__Unattended__InstallUnattended=true" \
    "Umbraco__CMS__Unattended__UpgradeUnattended=true" \
    "Umbraco__CMS__Unattended__UnattendedUserName=Admin" \
    "Umbraco__CMS__Unattended__UnattendedUserEmail=admin@dev.sporthalle-sulzerallee.ch" \
    "Umbraco__CMS__Unattended__UnattendedUserPassword=${DEV_UMBRACO_PASS}" \
    "uSync__Settings__ExportOnSave=false" \
    "Turnstile__SiteKey=1x00000000000000000000AA" \
    "Turnstile__SecretKey=1x0000000000000000000000000000000AA" \
  --output none

# ── Disable SCM basic auth on both apps ──────────────────────────────────────
for APP_NAME in "$DEV_APP" "$PROD_APP"; do
  echo "==> Disable SCM basic auth on $APP_NAME"
  az resource update \
    --resource-group "$RESOURCE_GROUP" --namespace Microsoft.Web \
    --parent "sites/${APP_NAME}" --resource-type basicPublishingCredentialsPolicies \
    --name scm --set properties.allow=false --output none
done

# ── Entra App Registration ────────────────────────────────────────────────────
echo "==> Entra App Registration ($APP_REG_NAME)"
APP_ID=$(az ad app create --display-name "$APP_REG_NAME" --query appId -o tsv)
az ad sp create --id "$APP_ID" --query id -o tsv >/dev/null || true
SP_OBJECT_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)

echo "==> Federated credential: GitHub Environment 'dev'"
az ad app federated-credential create --id "$APP_ID" --parameters "{
  \"name\":\"github-sporthalle-dev\",
  \"issuer\":\"https://token.actions.githubusercontent.com\",
  \"subject\":\"repo:${GH_REPO}:environment:dev\",
  \"audiences\":[\"api://AzureADTokenExchange\"]
}" --output none

echo "==> Federated credential: GitHub Environment 'prod'"
az ad app federated-credential create --id "$APP_ID" --parameters "{
  \"name\":\"github-sporthalle-prod\",
  \"issuer\":\"https://token.actions.githubusercontent.com\",
  \"subject\":\"repo:${GH_REPO}:environment:prod\",
  \"audiences\":[\"api://AzureADTokenExchange\"]
}" --output none

# ── Website Contributor role on both apps ─────────────────────────────────────
for APP_NAME in "$DEV_APP" "$PROD_APP"; do
  echo "==> Role assignment: Website Contributor on $APP_NAME"
  APP_SCOPE=$(az webapp show -g "$RESOURCE_GROUP" -n "$APP_NAME" --query id -o tsv)
  RA_GUID=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || python3 -c "import uuid;print(uuid.uuid4())")
  az rest --method put \
    --url "https://management.azure.com${APP_SCOPE}/providers/Microsoft.Authorization/roleAssignments/${RA_GUID}?api-version=2022-04-01" \
    --headers "Content-Type=application/json" \
    --body "{\"properties\":{\"roleDefinitionId\":\"/subscriptions/${SUBSCRIPTION}/providers/Microsoft.Authorization/roleDefinitions/de139f84-1756-47ae-9be6-808fbbe84772\",\"principalId\":\"${SP_OBJECT_ID}\",\"principalType\":\"ServicePrincipal\"}}" \
    --output none || echo "  (role assignment may already exist; continuing)"
done

TENANT_ID=$(az account show --query tenantId -o tsv)

cat <<EOF

Done. GitHub configuration (https://github.com/${GH_REPO}/settings):

  Repository secrets  (Settings → Secrets and variables → Actions → Repository secrets):
    AZURE_CLIENT_ID       = ${APP_ID}
    AZURE_TENANT_ID       = ${TENANT_ID}
    AZURE_SUBSCRIPTION_ID = ${SUBSCRIPTION}

  Create two GitHub Environments (Settings → Environments):
    'dev'   → Variables → AZURE_WEBAPP_NAME = ${DEV_APP}
    'prod'  → Variables → AZURE_WEBAPP_NAME = ${PROD_APP}

  Set via GitHub CLI:
    gh secret set AZURE_CLIENT_ID       --repo ${GH_REPO} --body "${APP_ID}"
    gh secret set AZURE_TENANT_ID       --repo ${GH_REPO} --body "${TENANT_ID}"
    gh secret set AZURE_SUBSCRIPTION_ID --repo ${GH_REPO} --body "${SUBSCRIPTION}"
    gh variable set AZURE_WEBAPP_NAME   --repo ${GH_REPO} --env dev  --body "${DEV_APP}"
    gh variable set AZURE_WEBAPP_NAME   --repo ${GH_REPO} --env prod --body "${PROD_APP}"

  Once KUDU_USER / KUDU_PASS secrets are no longer needed, delete them from GitHub.

Remaining manual steps:
  1. Set Brevo API key on both apps:
       az webapp config appsettings set -g ${RESOURCE_GROUP} -n ${DEV_APP}  --settings "Brevo__ApiKey=<key>"
       az webapp config appsettings set -g ${RESOURCE_GROUP} -n ${PROD_APP} --settings "Brevo__ApiKey=<key>"
  2. Set real Turnstile keys on the prod app:
       az webapp config appsettings set -g ${RESOURCE_GROUP} -n ${PROD_APP} --settings "Turnstile__SiteKey=<key>" "Turnstile__SecretKey=<key>"
  3. Disable ExportOnSave on the prod app (prevents uSync writing to non-persistent disk):
       az webapp config appsettings set -g ${RESOURCE_GROUP} -n ${PROD_APP} --settings "uSync__Settings__ExportOnSave=false"
  4. Push to a feature/* branch to trigger the first dev deploy.
     The dev DB starts empty — Umbraco installs itself unattended on first request (~2-3 min).
     uSync imports content types automatically (ImportOnStartup=true in appsettings.json).
EOF
