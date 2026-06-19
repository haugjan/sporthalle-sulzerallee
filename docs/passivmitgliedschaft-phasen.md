# Phasenplan: Passivmitgliedschaft

Erstellt: 2026-06-19  
Referenz: [passivmitgliedschaft-implementierungsplan.md](passivmitgliedschaft-implementierungsplan.md)

Die Umsetzung erfolgt in vier Phasen. Nach jeder Phase ist das Feature deploybar und nutzbar.

---

## Übersicht

| Phase | Inhalt | Ergebnis |
|-------|--------|----------|
| **1 – MVP** | Domain, DB-Migration, Repository, Use Cases, einfaches Formular, E-Mail via Brevo, Umbraco Content Type + Template | Registrierungen sind möglich; kein SVG, kein Wizard, kein CAPTCHA |
| **2 – Bodenplan** | SVG-Grid, Multi-Step-Wizard, Live-Zähler, VIP-Felder, Cloudflare Turnstile | Vollständige User Experience mit interaktivem Bodenplan |
| **3 – Admin-Bereich** | Razor Page `/passivmitglieder-admin`, Bezahlung markieren, Notizen, Excel- und AbaNinja-Export | Interne Verwaltung und Rechnungsexport |
| **4 – Homepage-Teaser** | Teaser-Abschnitt in `Home.cshtml` | Sichtbarkeit auf der Startseite |

---

## Phase 1: MVP

**Ziel:** Registrierungen sind möglich. Kein SVG, kein Wizard, kein CAPTCHA.

### Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Domain/PassivMitgliedschaft/DomainException.cs` | `DomainException`, `FieldAlreadyTakenException`, `MemberNotFoundException` |
| `Domain/PassivMitgliedschaft/FieldNumber.cs` | Value Object, validiert 1–300 |
| `Domain/PassivMitgliedschaft/MemberEmail.cs` | Value Object, normalisiert auf Lowercase |
| `Domain/PassivMitgliedschaft/MembershipLevel.cs` | Value Object mit Bronze / Silber / Gold |
| `Domain/PassivMitgliedschaft/PassivMitglied.cs` | Aggregate Root inkl. `Reconstitute()` für Repository |
| `Domain/PassivMitgliedschaft/Events/MemberRegisteredEvent.cs` | Einfacher Record |
| `Domain/PassivMitgliedschaft/Ports/IPassivMitgliederRepository.cs` | Repository-Port |
| `Domain/PassivMitgliedschaft/Ports/IEmailPort.cs` | E-Mail-Port |
| `Infrastructure/PassivMitgliedschaft/Persistence/PassivMitgliedDbRecord.cs` | PetaPoco-Record mit NPoco-Attributen |
| `Infrastructure/PassivMitgliedschaft/Persistence/PassivMitgliederMigration.cs` | `MigrationPlan` + `MigrationBase` + `IComponent` |
| `Infrastructure/PassivMitgliedschaft/Persistence/PassivMitgliederRepository.cs` | CRUD via PetaPoco / `IScopeProvider` |
| `Infrastructure/PassivMitgliedschaft/Email/BrevoEmailOptions.cs` | Options-Klasse |
| `Infrastructure/PassivMitgliedschaft/Email/BrevoEmailAdapter.cs` | Brevo REST API (kein SMTP) |
| `Infrastructure/PassivMitgliedschaft/Composition/PassivMitgliederComposer.cs` | DI-Registrierung (Phase-1-Teil) |
| `Application/PassivMitgliedschaft/RegisterMemberCommand.cs` | Command-Record |
| `Application/PassivMitgliedschaft/RegisterMemberUseCase.cs` | Prüft Feld, speichert, sendet E-Mail |
| `Application/PassivMitgliedschaft/FieldStatusDto.cs` | DTO + `FieldStatusesResult` |
| `Application/PassivMitgliedschaft/GetFieldStatusesQuery.cs` | Liefert belegte Felder (vipLabel = null in Phase 1) |
| `Presentation/PassivMitgliedschaft/Dtos/RegisterMemberRequest.cs` | API-Request-DTO |
| `Presentation/PassivMitgliedschaft/Dtos/FieldStatusResponse.cs` | API-Response-DTO |
| `Presentation/PassivMitgliedschaft/Controllers/PassivMitgliederController.cs` | `GET /felder`, `POST /register` (ohne CAPTCHA) |
| `Views/PassivMitgliedschaft.cshtml` | Page-Header + Stufen-Cards + einfaches Formular |
| `wwwroot/css/passivmitglied.css` | Phase-1-Styles (Header, Cards, Formular) |
| `uSync/v17/ContentTypes/passivmitgliedschaft.config` | Neuer Content Type |
| `uSync/v17/Templates/passivmitgliedschaft.config` | Neues Template |

### Geänderte Dateien

| Datei | Änderung |
|-------|---------|
| `uSync/v17/ContentTypes/homepage.config` | `<Structure>` um `passivMitgliedschaft` erweitern |
| `appsettings.json` | `"Brevo": { "ApiKey": "" }` hinzufügen |

### Implementierungsreihenfolge

1. Domain-Schicht (Exceptions → Value Objects → PassivMitglied → Ports)
2. Infrastructure: Migration + PassivMitgliedDbRecord + Repository
3. Infrastructure: BrevoEmailAdapter + BrevoEmailOptions
4. Application: RegisterMemberCommand + RegisterMemberUseCase + GetFieldStatusesQuery + FieldStatusDto
5. Infrastructure: PassivMitgliederComposer (Phase-1-DI)
6. Presentation: DTOs + Controller (GET /felder, POST /register)
7. appsettings.json anpassen
8. uSync-Configs (ContentType + Template)
9. Views/PassivMitgliedschaft.cshtml + passivmitglied.css
10. Lokaler Test: Registrierung, Brevo-E-Mail prüfen
11. Deploy + Azure `Brevo__ApiKey` setzen + End-to-End-Test
12. Umbraco Backoffice: Seite anlegen und publizieren

### Deployment-Checkliste Phase 1

- [ ] Azure App Service Environment Variable `Brevo__ApiKey` setzen
- [ ] DB-Migration läuft automatisch beim ersten App-Start
- [ ] uSync importiert ContentType + Template automatisch
- [ ] Umbraco Backoffice: Seite "Passivmitgliedschaft" anlegen, unter Homepage verschachteln, publizieren
- [ ] End-to-End: Formular ausfüllen, absenden, Brevo-E-Mail prüfen (BCC an alle 3 Adressen)

---

## Phase 2: Bodenplan

**Ziel:** Vollständige User Experience mit interaktivem SVG-Bodenplan, Multi-Step-Wizard und CAPTCHA.

### Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Domain/PassivMitgliedschaft/VipField.cs` | Torraum, Anspielkreis, Anspielpunkte |
| `Domain/PassivMitgliedschaft/Ports/ICaptchaPort.cs` | CAPTCHA-Port |
| `Infrastructure/PassivMitgliedschaft/Captcha/TurnstileOptions.cs` | SiteKey + SecretKey |
| `Infrastructure/PassivMitgliedschaft/Captcha/TurnstileCaptchaAdapter.cs` | Cloudflare Turnstile Verifikation |
| `wwwroot/js/passivmitglied.js` | SVG-Grid, Wizard (6 Schritte), Live-Zähler, Touch-Support |
| `wwwroot/media/unihockey-boden.svg` | Spielfeldgrafik (viewBox 840×440) |

### Geänderte Dateien

| Datei | Änderung |
|-------|---------|
| `Infrastructure/Composition/PassivMitgliederComposer.cs` | Turnstile-DI ergänzen |
| `Presentation/Controllers/PassivMitgliederController.cs` | CAPTCHA-Prüfung im `/register`-Endpoint |
| `Presentation/Dtos/RegisterMemberRequest.cs` | `CaptchaToken`-Feld hinzufügen |
| `Infrastructure/Email/BrevoEmailAdapter.cs` | VipLabel in `fieldDesc` ergänzen |
| `Application/GetFieldStatusesQuery.cs` | `VipField.GetLabel()` einbauen |
| `Views/PassivMitgliedschaft.cshtml` | SVG inline einbinden, Modal hinzufügen, Formular ersetzen |
| `wwwroot/css/passivmitglied.css` | Wizard, Modal, SVG-Overlay, Stufen-Karten erweitern |
| `appsettings.json` | `"Turnstile": { "SiteKey": "", "SecretKey": "" }` hinzufügen |

### Implementierungsreihenfolge

1. VipField.cs + ICaptchaPort.cs
2. TurnstileOptions + TurnstileCaptchaAdapter
3. Composer ergänzen
4. RegisterMemberRequest: CaptchaToken hinzufügen
5. Controller: CAPTCHA-Prüfung
6. BrevoEmailAdapter: VipLabel
7. GetFieldStatusesQuery: VipField.GetLabel()
8. unihockey-boden.svg erstellen
9. passivmitglied.js (SVG-Grid, Wizard, Zähler)
10. CSS erweitern (Wizard, Modal, Grid)
11. PassivMitgliedschaft.cshtml erweitern
12. appsettings.json: Turnstile-Sektion
13. Mobile-Tests (iOS Safari, Android Chrome)
14. Deploy + Azure `Turnstile__SiteKey` + `Turnstile__SecretKey` setzen + End-to-End-Test

### Deployment-Checkliste Phase 2

- [ ] Azure: `Turnstile__SiteKey` und `Turnstile__SecretKey` setzen (aus Cloudflare Dashboard)
- [ ] `wwwroot/media/unihockey-boden.svg` im Repository vorhanden
- [ ] End-to-End: SVG lädt, Feld klicken, Wizard 6 Schritte, Turnstile besteht, E-Mail mit VIP-Label kommt an
- [ ] Mobile-Test: iOS Safari, Android Chrome

---

## Phase 3: Admin-Bereich

**Ziel:** Interne Verwaltung der Mitglieder mit Bezahlstatus, Notizen und Exportfunktionen.

### Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Domain/PassivMitgliedschaft/Ports/IExcelPort.cs` | Excel-Port |
| `Domain/PassivMitgliedschaft/Ports/IAbaninjaCsvPort.cs` | AbaNinja-CSV-Port |
| `Application/PassivMitgliedschaft/AdminService.cs` | MarkAsPaid, UpdateNotes, Export |
| `Infrastructure/PassivMitgliedschaft/Excel/ClosedXmlExcelAdapter.cs` | Excel-Export (ClosedXML) |
| `Infrastructure/PassivMitgliedschaft/Excel/AbaninjaCsvAdapter.cs` | CSV-Export für AbaNinja |
| `Pages/PassivMitgliederAdmin.cshtml` | Admin-Tabelle, Filter, Export-Buttons |
| `Pages/PassivMitgliederAdmin.cshtml.cs` | PageModel, geschützt mit BackOffice-Auth |

### Geänderte Dateien

| Datei | Änderung |
|-------|---------|
| `Infrastructure/Composition/PassivMitgliederComposer.cs` | Excel/CSV/Admin-DI ergänzen |
| `Presentation/Controllers/PassivMitgliederController.cs` | Admin-Endpunkte hinzufügen (`/paid`, `/notes`, `/admin/members`, `/admin/export/*`) |
| `Directory.Packages.props` | `ClosedXML` Version hinzufügen |
| `SporthalleWeb.csproj` | `<PackageReference Include="ClosedXML" />` hinzufügen |
| `Program.cs` | `builder.Services.AddRazorPages()` + `app.MapRazorPages()` hinzufügen |

### Implementierungsreihenfolge

1. IExcelPort + IAbaninjaCsvPort
2. AdminService
3. Directory.Packages.props + SporthalleWeb.csproj (ClosedXML)
4. ClosedXmlExcelAdapter + AbaninjaCsvAdapter
5. Composer ergänzen
6. Controller: Admin-Endpunkte
7. Program.cs: Razor Pages aktivieren
8. Pages/PassivMitgliederAdmin.cshtml + .cshtml.cs
9. Deploy + Admin-Bereich testen

### Deployment-Checkliste Phase 3

- [ ] `ClosedXML` NuGet in `SporthalleWeb.csproj` und `Directory.Packages.props`
- [ ] `Program.cs`: `AddRazorPages()` + `MapRazorPages()` vorhanden
- [ ] Admin-Bereich unter `/passivmitglieder-admin` nach Umbraco-Login erreichbar
- [ ] Excel-Export: Datei öffnet sich korrekt, alle Spalten vorhanden
- [ ] AbaNinja-CSV: Datei in AbaNinja unter "Kontakte > Importieren" hochladen, Felder-Mapping prüfen
- [ ] Bezahlung markieren: AJAX-Call, visuelles Feedback
- [ ] Notiz speichern: blur-Event, kein Seitenneulade

---

## Phase 4: Homepage-Teaser

**Ziel:** Das Feature ist prominent auf der Startseite verlinkt.

### Geänderte Dateien

| Datei | Änderung |
|-------|---------|
| `Views/Home.cshtml` | Teaser-Block nach dem Hero einfügen |
| `wwwroot/css/sporthalle.css` oder `passivmitglied.css` | `.passiv-teaser`-Styles |

### Implementierungsreihenfolge

1. CSS: `.passiv-teaser`-Block (Vereinsrot, Noise-Overlay, CTA-Button)
2. `Views/Home.cshtml`: Teaser einfügen
3. Deploy + visueller Check Desktop und Mobile

### Deployment-Checkliste Phase 4

- [ ] Teaser auf Startseite sichtbar
- [ ] Link `/passivmitgliedschaft` führt zur richtigen Seite
- [ ] Responsive auf Mobile korrekt
