#requires -Version 7
# ============================================================================
#  Sporthalle Sulzerallee: Microsoft-Login (Entra ID) fuer das Umbraco-Backoffice einrichten.
#  Eigene App-Registrierung mit Redirect-URIs (interaktives Web-Login), NICHT
#  die Graph-Mail-App.
#  Nur bestehende Umbraco-User mit @sporthalle-sulzerallee.ch werden verknuepft;
#  lokales Passwort-Login ist gesperrt (DenyLocalLogin).
#  Als Entra-Admin ausfuehren.
# ============================================================================

# ---------------------------- KONFIG ----------------------------------------
$TenantId      = ''   # sporthalle-sulzerallee.ch Tenant-ID
$AppName       = 'Sporthalle Sulzerallee Backoffice Login'
$CallbackPath  = '/umbraco-entra-signin'
$SignoutPath   = '/umbraco-entra-signout'
$AdminHosts    = @(
    'https://app-sporthalle-sulzerallee.azurewebsites.net',      # PROD
    'https://app-sporthalle-sulzerallee-dev.azurewebsites.net',  # DEV
    'https://localhost:44343'                                     # lokal
)
# ----------------------------------------------------------------------------

$RedirectUris = $AdminHosts | ForEach-Object { "$_$CallbackPath" }
$LogoutUris   = $AdminHosts | ForEach-Object { "$_$SignoutPath" }

Write-Host "`n=================================================================" -ForegroundColor Magenta
Write-Host " App-Registrierung im Entra-Portal (portal.azure.com > Entra > App registrations):" -ForegroundColor Magenta
Write-Host "   1) New registration: Name '$AppName', 'Accounts in this organizational directory only' (Single tenant)." -ForegroundColor Magenta
Write-Host "      Platform 'Web'. Redirect-URIs (alle eintragen):" -ForegroundColor Magenta
$RedirectUris | ForEach-Object { Write-Host "        - $_" -ForegroundColor Magenta }
Write-Host "      Front-channel logout URL (PROD): $($LogoutUris[0])" -ForegroundColor Magenta
Write-Host "      -> Application (client) ID + Directory (tenant) ID notieren." -ForegroundColor Magenta
Write-Host "   2) Authentication: 'ID tokens' NICHT noetig (Authorization Code Flow). Keine impliziten Grants." -ForegroundColor Magenta
Write-Host "   3) Token configuration > Add optional claim > ID > 'email' (und 'upn'), Haken 'Turn on the Microsoft Graph email permission'." -ForegroundColor Magenta
Write-Host "   4) API permissions: Microsoft Graph > DELEGATED > openid, profile, email. Admin consent erteilen." -ForegroundColor Magenta
Write-Host "   5) Certificates & secrets > New client secret > VALUE (Geheimnis) sofort kopieren." -ForegroundColor Magenta
Write-Host "=================================================================" -ForegroundColor Magenta

if ([string]::IsNullOrWhiteSpace($TenantId)) { $TenantId = Read-Host 'Directory (tenant) ID' }
$AppId = Read-Host 'Application (client) ID der neuen App-Registrierung'
if ([string]::IsNullOrWhiteSpace($AppId)) { throw 'Ohne App-ID kann nichts gesetzt werden.' }

$CsprojPath = 'C:\development\sporthalle-sulzerallee\Sporthalle-Sulzerallee\src\SporthalleWeb\SporthalleWeb.csproj'

Write-Host "`n--- Lokal (user-secrets) ---" -ForegroundColor Green
Write-Host "  dotnet user-secrets set `"BackOfficeAuth:TenantId`" `"$TenantId`" --project `"$CsprojPath`""
Write-Host "  dotnet user-secrets set `"BackOfficeAuth:ClientId`" `"$AppId`" --project `"$CsprojPath`""
Write-Host "  dotnet user-secrets set `"BackOfficeAuth:ClientSecret`" `"<CLIENT-SECRET-VALUE>`" --project `"$CsprojPath`""

Write-Host "`n--- DEV App Service (app-sporthalle-sulzerallee-dev) ---" -ForegroundColor Green
Write-Host "  az webapp config appsettings set -g Sporthalle-Sulzerallee -n app-sporthalle-sulzerallee-dev --settings ``"
Write-Host "    BackOfficeAuth__TenantId=$TenantId BackOfficeAuth__ClientId=$AppId BackOfficeAuth__ClientSecret=<SECRET>"

Write-Host "`n--- PROD App Service (app-sporthalle-sulzerallee) ---" -ForegroundColor Green
Write-Host "  az webapp config appsettings set -g Sporthalle-Sulzerallee -n app-sporthalle-sulzerallee --settings ``"
Write-Host "    BackOfficeAuth__TenantId=$TenantId BackOfficeAuth__ClientId=$AppId BackOfficeAuth__ClientSecret=<SECRET>"

Write-Host "`nHinweis: Backoffice-User muessen ihre @sporthalle-sulzerallee.ch-E-Mail als Umbraco-User-E-Mail" -ForegroundColor Yellow
Write-Host "hinterlegt haben, sonst greift die Verknuepfung nicht (keine Auto-Anlage)." -ForegroundColor Yellow
Write-Host "Break-glass: BackOfficeAuth-Settings leeren -> klassisches Umbraco-Login ist wieder aktiv." -ForegroundColor DarkGray
