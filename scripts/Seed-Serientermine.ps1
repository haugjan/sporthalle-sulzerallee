<#
.SYNOPSIS
    Erstellt Serientermine (Recurring Slots) über die Dev-API der Sporthalle.

.DESCRIPTION
    Nutzt zwei Dev-only Endpunkte (nur in Development, AllowAnonymous):
      GET  /api/admin/reservierungen/serientermine/members?q=...   -> HallMember-Suche (ID-Auflösung)
      POST /api/admin/reservierungen/serientermine/seed            -> Serientermin anlegen

    Die HallMember werden anhand des Suchbegriffs (Team) automatisch aufgelöst und
    den Terminen zugewiesen. Alle Termine werden extern sichtbar angelegt
    (ShowTitlePublic = true, Typ Recurring).

    ACHTUNG: Mehrfaches Ausführen legt DUPLIKATE an (keine Dedup-Logik).

.PARAMETER BaseUrl
    Basis-URL der laufenden lokalen Instanz. Default: https://localhost:22540

.PARAMETER SeriesStart
    Serienbeginn (yyyy-MM-dd). Default: 2026-08-16

.PARAMETER SeriesEnd
    Serienende (yyyy-MM-dd). Default: 2027-05-31

.EXAMPLE
    pwsh ./scripts/Seed-Serientermine.ps1
    pwsh ./scripts/Seed-Serientermine.ps1 -BaseUrl https://localhost:44343
#>
[CmdletBinding()]
param(
    [string] $BaseUrl     = "https://localhost:22540",
    [string] $SeriesStart = "2026-08-16",
    [string] $SeriesEnd   = "2027-05-31"
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd("/")

# Selbstsigniertes localhost-Zertifikat akzeptieren (nur lokal).
$irmCommon = @{ SkipCertificateCheck = $true }

# Farb-Zuordnung pro Team
$Colors = @{
    HCR        = "#FFC107"   # Gelb
    WinU       = "#000000"   # Schwarz
    "Red Ants" = "#D32F2F"   # Rot
}

# Wochentag: Sonntag=0, Montag=1, Dienstag=2, ... (System.DayOfWeek)
# Serientermine Sulzerallee (Plan ab 16.08.2026)
$Slots = @(
    @{ Team = "HCR";  Title = "HCR U13";       Weekday = 1; From = "17:30"; To = "19:00" }
    @{ Team = "HCR";  Title = "HCR U14 A";     Weekday = 1; From = "19:00"; To = "20:30" }
    @{ Team = "HCR";  Title = "HCR NLA";       Weekday = 1; From = "20:30"; To = "22:00" }
    @{ Team = "WinU"; Title = "WinU U16C";     Weekday = 2; From = "17:30"; To = "19:00" }
    @{ Team = "WinU"; Title = "WinU U18C";     Weekday = 2; From = "19:00"; To = "20:30" }
    @{ Team = "WinU"; Title = "WinU 2. Liga";  Weekday = 2; From = "20:30"; To = "22:00" }
)

function Resolve-MemberId([string] $team) {
    $uri = "$BaseUrl/api/admin/reservierungen/serientermine/members?q=$([uri]::EscapeDataString($team))"
    $found = @(Invoke-RestMethod @irmCommon -Method Get -Uri $uri)
    if ($found.Count -eq 0) {
        throw "Kein HallMember fuer '$team' gefunden. Bitte Member zuerst anlegen."
    }
    $chosen = $found[0]
    $label  = if ($chosen.name) { $chosen.name } else { "$($chosen.contactFirstName) $($chosen.contactLastName)".Trim() }
    if ($found.Count -gt 1) {
        Write-Warning "Mehrere Treffer fuer '$team' ($($found.Count)). Verwende ersten: Id=$($chosen.id) '$label' <$($chosen.email)>"
    } else {
        Write-Host "  Member '$team' -> Id=$($chosen.id) '$label' <$($chosen.email)>" -ForegroundColor DarkGray
    }
    return [int] $chosen.id
}

Write-Host "Serientermine anlegen ($SeriesStart bis $SeriesEnd) auf $BaseUrl" -ForegroundColor Cyan
Write-Host ""

# Member-IDs einmalig pro Team aufloesen
$memberIds = @{}
foreach ($team in ($Slots.Team | Select-Object -Unique)) {
    $memberIds[$team] = Resolve-MemberId $team
}
Write-Host ""

$weekdayNames = @("So","Mo","Di","Mi","Do","Fr","Sa")
$created = 0
foreach ($s in $Slots) {
    $body = @{
        title           = $s.Title
        weekday         = $s.Weekday
        from            = $s.From
        to              = $s.To
        seriesStart     = $SeriesStart
        seriesEnd       = $SeriesEnd
        color           = $Colors[$s.Team]
        notes           = $null
        isBlocker       = $false
        memberId        = $memberIds[$s.Team]
        showTitlePublic = $true
    } | ConvertTo-Json

    $uri = "$BaseUrl/api/admin/reservierungen/serientermine/seed"
    $res = Invoke-RestMethod @irmCommon -Method Post -Uri $uri -ContentType "application/json" -Body $body

    $wd = $weekdayNames[$s.Weekday]
    Write-Host ("OK  {0,-13} {1} {2}-{3}  -> SerieId={4}, erstellt={5}, uebersprungen={6}" -f `
        $s.Title, $wd, $s.From, $s.To, $res.recurringSlotId, $res.created, $res.skipped) -ForegroundColor Green
    $created++
}

Write-Host ""
Write-Host "Fertig: $created Serientermine angelegt." -ForegroundColor Cyan
