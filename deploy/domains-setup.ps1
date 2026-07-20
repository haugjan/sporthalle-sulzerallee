#Requires -Version 7
#
# Bind custom domains and provision free managed TLS certificates.
#
# Prerequisites:
#   1. azure-setup.ps1 has been run (dev app exists)
#   2. DNS records (see output below) are set at your registrar
#   3. DNS has propagated  (check: Resolve-DnsName sporthalle-sulzerallee.ch)
#
# Run:
#   az login
#   pwsh deploy/domains-setup.ps1

$ErrorActionPreference = 'Stop'

$Subscription   = '5a44bc29-c597-4d08-9ebf-212c359e3606'
$ResourceGroup  = 'Sporthalle-Sulzerallee'
$ProdApp        = 'app-sporthalle-sulzerallee'
$DevApp         = 'app-sporthalle-sulzerallee-dev'
$ProdApex       = 'sporthalle-sulzerallee.ch'
$ProdWww        = 'www.sporthalle-sulzerallee.ch'
$DevDomain      = 'dev.sporthalle-sulzerallee.ch'

az account set --subscription $Subscription

Write-Host '==> Fetching domain verification IDs...'
$ProdVerif = az webapp show -g $ResourceGroup -n $ProdApp `
    --query customDomainVerificationId -o tsv

$BindDev  = $false
$DevVerif = ''
try {
    $null = az webapp show -g $ResourceGroup -n $DevApp 2>$null
    $DevVerif = az webapp show -g $ResourceGroup -n $DevApp `
        --query customDomainVerificationId -o tsv
    $BindDev = $true
} catch {
    Write-Warning "Dev app '$DevApp' not found. Run azure-setup.ps1 first if you want $DevDomain."
}

Write-Host @"

Required DNS records at your registrar / DNS provider:

  TYPE   NAME        VALUE
  ------------------------------------------------------------------------------
  A      @           51.107.58.161
  TXT    asuid       $ProdVerif
  CNAME  www         $ProdApp.azurewebsites.net
  TXT    asuid.www   $ProdVerif
"@

if ($BindDev) {
    Write-Host @"
  CNAME  dev         $DevApp.azurewebsites.net
  TXT    asuid.dev   $DevVerif
"@
}

Write-Host @"

  The A record targets the current Azure IP. Update it if the site becomes
  unreachable after an Azure maintenance event (re-run: Resolve-DnsName $ProdApp.azurewebsites.net).

  www -> apex redirect is handled in app middleware (301 in Program.cs). www
  still needs its own TLS cert so the browser can establish the HTTPS connection
  before receiving the redirect response.

"@

$confirm = Read-Host "DNS records added and propagated? (Check: Resolve-DnsName $ProdApex) [y/N]"
if ($confirm -notin @('y', 'Y')) {
    Write-Host 'Aborted. Re-run after DNS is set up.'
    exit 0
}

function Invoke-BindDomain {
    param(
        [string] $App,
        [string] $Hostname
    )
    Write-Host ''
    Write-Host "==> Bind hostname: $Hostname -> $App"
    az webapp config hostname add `
        --resource-group $ResourceGroup --webapp-name $App --hostname $Hostname

    Write-Host "==> Managed TLS certificate for $Hostname (may take 1-3 minutes)"
    $thumb = az webapp config ssl create `
        --resource-group $ResourceGroup --name $App --hostname $Hostname `
        --query thumbprint -o tsv

    Write-Host "==> Bind TLS (SNI) for $Hostname  [thumbprint: $thumb]"
    az webapp config ssl bind `
        --resource-group $ResourceGroup --name $App `
        --certificate-thumbprint $thumb --ssl-type SNI
}

Invoke-BindDomain $ProdApp $ProdApex
Invoke-BindDomain $ProdApp $ProdWww

if ($BindDev) {
    Invoke-BindDomain $DevApp $DevDomain
}

Write-Host ''
Write-Host 'Done. Active domains:'
Write-Host "  https://$ProdApex"
Write-Host "  https://$ProdWww  ->  301 -> https://$ProdApex  (app middleware)"
if ($BindDev) { Write-Host "  https://$DevDomain" }
