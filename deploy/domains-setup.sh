#!/usr/bin/env bash
#
# Bind custom domains and provision free managed TLS certificates.
#
# Prerequisites:
#   1. azure-setup.sh has been run (dev app exists)
#   2. DNS records below are set at your registrar
#   3. DNS has propagated (check: nslookup sporthalle-sulzerallee.ch)
#
# Run:
#   az login
#   bash deploy/domains-setup.sh
#
set -euo pipefail

SUBSCRIPTION="5a44bc29-c597-4d08-9ebf-212c359e3606"
RESOURCE_GROUP="Sporthalle-Sulzerallee"
PROD_APP="app-sporthalle-sulzerallee"
DEV_APP="app-sporthalle-sulzerallee-dev"
PROD_APEX="sporthalle-sulzerallee.ch"
PROD_WWW="www.sporthalle-sulzerallee.ch"
DEV_DOMAIN="dev.sporthalle-sulzerallee.ch"

az account set --subscription "$SUBSCRIPTION"

echo "==> Fetching domain verification IDs..."
PROD_VERIF=$(az webapp show -g "$RESOURCE_GROUP" -n "$PROD_APP" \
  --query customDomainVerificationId -o tsv)

DEV_VERIF=""
BIND_DEV=false
if az webapp show -g "$RESOURCE_GROUP" -n "$DEV_APP" &>/dev/null; then
  DEV_VERIF=$(az webapp show -g "$RESOURCE_GROUP" -n "$DEV_APP" \
    --query customDomainVerificationId -o tsv)
  BIND_DEV=true
else
  echo "WARNING: Dev app '$DEV_APP' not found. Run azure-setup.sh first if you want $DEV_DOMAIN."
  echo "         Skipping dev domain setup."
fi

cat <<DNS

Required DNS records at your registrar / DNS provider:

  TYPE   NAME        VALUE
  ──────────────────────────────────────────────────────────────────────────────
  A      @           51.107.58.161
  TXT    asuid       ${PROD_VERIF}
  CNAME  www         ${PROD_APP}.azurewebsites.net
  TXT    asuid.www   ${PROD_VERIF}
DNS

if [ "$BIND_DEV" = "true" ]; then
cat <<DNS
  CNAME  dev         ${DEV_APP}.azurewebsites.net
  TXT    asuid.dev   ${DEV_VERIF}
DNS
fi

cat <<'DNS'

  The A record points the apex domain to the current Azure App Service IP.
  Azure may reassign this IP if the service plan is moved; update the A record
  if the site becomes unreachable after an Azure maintenance event.

  www → apex redirect is handled in app middleware (Program.cs): a 301 is
  issued before any other processing, so you need a TLS cert for www even
  though all traffic is immediately forwarded to the apex.

DNS

read -rp "DNS records added and propagated? Check with 'nslookup ${PROD_APEX}'. Proceed? [y/N] " CONFIRM
if [[ "${CONFIRM,,}" != "y" ]]; then
  echo "Aborted. Re-run after DNS is set up."; exit 0
fi

# Helper: bind a hostname, create a managed cert, and bind the cert (all idempotent).
bind_domain() {
  local app="$1" hostname="$2"
  echo ""
  echo "==> Bind hostname: $hostname → $app"
  az webapp config hostname add \
    --resource-group "$RESOURCE_GROUP" --webapp-name "$app" --hostname "$hostname"

  echo "==> Managed TLS certificate for $hostname (may take 1-3 minutes)"
  local thumb
  thumb=$(az webapp config ssl create \
    --resource-group "$RESOURCE_GROUP" --name "$app" --hostname "$hostname" \
    --query thumbprint -o tsv)

  echo "==> Bind TLS (SNI) for $hostname (thumbprint: $thumb)"
  az webapp config ssl bind \
    --resource-group "$RESOURCE_GROUP" --name "$app" \
    --certificate-thumbprint "$thumb" --ssl-type SNI
}

bind_domain "$PROD_APP" "$PROD_APEX"
bind_domain "$PROD_APP" "$PROD_WWW"

if [ "$BIND_DEV" = "true" ]; then
  bind_domain "$DEV_APP" "$DEV_DOMAIN"
fi

echo ""
echo "Done. Active domains:"
echo "  https://${PROD_APEX}"
echo "  https://${PROD_WWW}  →  301 → https://${PROD_APEX}  (app middleware)"
[ "$BIND_DEV" = "true" ] && echo "  https://${DEV_DOMAIN}"
