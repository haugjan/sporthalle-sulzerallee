# Reservierung: Phasenplan (MVP-First)

Ergänzungsdokument zu [`reservierung-implementierungsplan.md`](reservierung-implementierungsplan.md).
Der Hauptplan beschreibt das Ziel-System vollständig. Dieses Dokument legt fest,
in welcher Reihenfolge die Teile gebaut werden, sodass nach jeder Phase etwas
Lauffähiges existiert.

---

## Leitprinzip

> Jede Phase endet mit einem **deployten, testbaren Zustand**.
> Nichts baut auf unfertigen Teilen der nächsten Phase auf.

---

## Phase 1 — Belegungs-Kalender (Lesend)

**Ziel:** Die `/reservierung`-Seite existiert und zeigt den Wochenkalender
mit bestehenden Buchungen. Kein Buchungsfluss, kein Auth.

**Referenz Hauptplan:** Abschnitt 3.3 (uSync), 4.2 (Kalender-Rendering), Dateistruktur §3.

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | `TimeSlot`, `BookingStatus`, `BookingSlot` (nur Read-Felder), `DomainException` |
| Infra · DB | `BookingSlotRecord`, `ReservierungMigration` (nur `BookingSlots`-Tabelle) |
| Infra · Repo | `BookingSlotRepository` (nur `GetForWeekAsync`) |
| Application | `GetWeekSlotsQuery`, `WeekSlotDto` |
| Presentation | `ReservierungController` (nur `GET /api/reservierung/wochen-slots`), `WeekSlotResponse` |
| Infra · Composition | `ReservierungComposer` (minimal: Migration + Repository + Query) |
| Umbraco | uSync `reservierung.config`, Template `Views/Reservierung.cshtml` |
| Frontend | `reservierung.css`, `reservierung.js` (Kalender-Grid, Wochen-Navigation, Farb-Codierung) |

**Was wird bewusst weggelassen:** Auth, Buchungsformular, E-Mail, Admin.

**Testbar nach Phase 1:**
- Seite unter `/reservierung` aufrufbar
- `GET /api/reservierung/wochen-slots?von=YYYY-MM-DD` liefert JSON
- Im Umbraco-Backoffice manuell einen `BookingSlot`-Datensatz in DB eintragen
  → erscheint farbig im Kalender

---

## Phase 2 — Buchungsanfrage (ohne Auth)

**Ziel:** Nutzer kann eine Buchungsanfrage stellen. Er gibt Name, E-Mail und
Kontaktdaten direkt im Formular ein (kein Magic-Link-Flow). Admin erhält eine
BCC-E-Mail. Buchung landet mit Status `Provisorisch` in der DB.

**Referenz Hauptplan:** Abschnitt 2.2 (`CreateBookingUseCase`), 4.3 (Schritt 1–4,
vereinfacht: Auth-Subflow = inline Kontaktformular), 5.1 (Admin-BCC).

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | `HallRenter`, `RenterEmail`, `RenterType`, `SlotConflictException` |
| Infra · DB | `HallRenterRecord` (Tabelle ergänzen in Migration) |
| Infra · Repo | `HallRenterRepository` (`FindByEmailAsync`, `SaveAsync`), `BookingSlotRepository` (`GetActiveOverlapsAsync`, `SaveAsync`) |
| Application | `CreateBookingCommand`, `CreateBookingUseCase` (vereinfacht: erstellt HallRenter on-the-fly), `RegisterRenterCommand`, `RegisterRenterUseCase` |
| Infra · Email | `BrevoEmailOptions`, einfacher inline-HTTP-Aufruf (kein Adapter-Interface nötig) für Admin-BCC |
| Presentation | `POST /api/reservierung/buchungen` (Kontaktdaten + Slot), `CreateBookingRequest` |
| Frontend | Buchungs-Picker (Schritte 1–4, Auth-Schritt = Kontaktformular mit Name/E-Mail) |

**Was wird bewusst weggelassen:** Magic Link, Session, vollständiger E-Mail-Adapter,
Turnstile CAPTCHA.

**Testbar nach Phase 2:**
- Nutzer wählt Zeitblock, füllt Formular aus, sendet Anfrage
- Buchung erscheint in DB als `Provisorisch`
- Admin erhält BCC-E-Mail an `jan.haug@sporthalle-sulzerallee.ch`
- Belegter Slot erscheint sofort im Kalender

---

## Phase 3 — E-Mail-System (Brevo)

**Ziel:** Alle E-Mail-Typen laufen korrekt über den `BrevoBookingEmailAdapter`
(Template ID 1). Mieter erhalten Bestätigung, Admin kann ablehnen und der Mieter
wird per E-Mail informiert.

**Referenz Hauptplan:** Abschnitt 3.2 (Brevo-Adapter), Ports `IBookingEmailPort`.

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | Port `IBookingEmailPort` |
| Infra · Email | `BrevoBookingEmailAdapter` (alle 5 Mail-Typen), BCC-Logik |
| Application | `CreateBookingUseCase` erweitern (ruft jetzt `IBookingEmailPort` auf), `ConfirmBookingUseCase`, `RejectBookingUseCase` |
| Presentation | `POST /admin/buchungen/{id}/bestaetigen`, `POST /admin/buchungen/{id}/ablehnen` (vereinfacht, noch kein Auth-Schutz) |

**Testbar nach Phase 3:**
- Buchungsanfrage: Mieter erhält provisorische Bestätigung, Admin BCC
- Admin ruft Bestätigungs-Endpoint auf: Mieter erhält Bestätigungs-Mail
- Admin ruft Ablehnungs-Endpoint auf: Mieter erhält Ablehnungs-Mail

---

## Phase 4 — Magic-Link Auth

**Ziel:** Wiederkehrende Mieter melden sich per Magic Link an und müssen
ihre Daten nicht neu eingeben. Sessions über HttpOnly-Cookie.

**Referenz Hauptplan:** Abschnitt 2.1 (Auth Use Cases), Tabellen `MagicLinkTokens`,
`HallSessions`.

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | `MagicLinkToken`, `HallSession`, Port `IMagicLinkTokenRepository` |
| Infra · DB | `MagicLinkTokenRecord`, `HallSessionRecord` (in Migration ergänzen) |
| Infra · Repo | `MagicLinkTokenRepository` (inkl. Session-Methoden) |
| Application | `SendMagicLinkUseCase`, `ValidateMagicLinkUseCase` |
| Infra · Email | `SendMagicLinkAsync` im Brevo-Adapter ergänzen |
| Presentation | `POST /magic-link`, `POST /auth/validate`, `POST /auth/logout`, `GET /me` |
| Frontend | Auth-Subflow in Schritt 4: bekannte E-Mail → Magic-Link-Spinner; Session-Banner |

**Testbar nach Phase 4:**
- Bekannte E-Mail: Magic-Link-Mail kommt an, Link setzt Cookie, Buchung wird unter Profil gespeichert
- Unbekannte E-Mail: Weiterleitung zum Registrierungsformular

---

## Phase 5 — Admin-Dashboard

**Ziel:** Admin hat eine geschützte Übersichtsseite für alle pendenten Buchungen
mit Freigabe-Workflow, Preisanpassung und Audit-Log.

**Referenz Hauptplan:** Abschnitt 5.1 (Freigabe-Dashboard), 5.4 (Audit-Log).

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | Port `IBookingAuditRepository`, `BookingAuditLog` |
| Infra · DB | `BookingAuditLogRecord` (in Migration ergänzen) |
| Infra · Repo | `BookingAuditRepository` |
| Application | `BookingAdminService` (Cancel, AdjustPrice, GetForExport), Audit-Calls in ConfirmBookingUseCase / RejectBookingUseCase ergänzen |
| Presentation | `ReservierungAdminController` (vollständig, `[UmbracoAdminAuthorize]`), `ReservierungAdmin.cshtml` + PageModel, `BookingAdminResponse` |
| Presentation | `GET /admin/pendente`, `DELETE /admin/buchungen/{id}`, `POST /admin/buchungen/{id}/preis`, `GET /admin/audit?entityId=` |

**Testbar nach Phase 5:**
- Admin-Seite unter `/reservierung-admin` aufrufbar (Umbraco-Login erforderlich)
- Pendente Buchungen tabellarisch einsehbar
- Bestätigen, Ablehnen, Preis anpassen funktionieren mit korrekten E-Mail-Benachrichtigungen
- Audit-Log zeigt alle Statusübergänge

---

## Phase 6 — Umbraco-Konfiguration

**Ziel:** Preis pro Block, Öffnungszeiten, buchbare Dauern und Anlass-Optionen
sind im Umbraco-Backoffice pflegbar statt hart im Code.

**Referenz Hauptplan:** Abschnitt 3.1 (Konfigurations-Knoten), Port `IHallConfigurationPort`.

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | Port `IHallConfigurationPort` |
| Infra | `UmbracoHallConfigurationAdapter` |
| Umbraco | uSync `reservierungKonfiguration.config` |
| Application | `CreateBookingUseCase`, `GetAvailableDaysQuery`, `GetAvailableTimeSlotsQuery` auf `IHallConfigurationPort` umstellen |

**Testbar nach Phase 6:**
- Konfigurations-Knoten im Backoffice anlegen
- Preis ändern → nächste Buchungsanfrage zeigt neuen Preis

---

## Phase 7 — Serien-Buchungen & Schulferien

**Ziel:** Admin kann wiederkehrende Hallenbelegungen (z. B. Wochentraining)
als Serien anlegen. Schulferien-Perioden werden aus dem Generator ausgeschlossen.

**Referenz Hauptplan:** Abschnitt 2.3 (Seriengenerator), Tabellen `RecurringRules`,
`SchoolHolidays`, Abschnitt 5.2 (Schulferien-Verwaltung).

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Domain | `RecurringRule`, `SchoolHoliday`, Port `IRecurringRuleRepository`, Port `ISchoolHolidayRepository` |
| Infra · DB | `RecurringRuleRecord`, `SchoolHolidayRecord` (in Migration ergänzen) |
| Infra · Repo | `RecurringRuleRepository`, `SchoolHolidayRepository` |
| Application | `CreateRecurringRuleCommand`, `CreateRecurringRuleUseCase` (Generator mit IntervalWeeks + ExcludeSchoolHolidays) |
| Presentation | `POST /admin/serien-regeln`, Schulferien-Endpoints, Admin-UI-Erweiterung (Serien + Ferien) |

**Testbar nach Phase 7:**
- Serienregel anlegen → generiert Einzel-Slots in DB
- Slots mit Admin-Farbe erscheinen farbig im Kalender
- Schulferienperiode eintragen, Serienregel mit ExcludeSchoolHolidays → Feriendaten leer

---

## Phase 8 — Buchungs-Picker & Mobile

**Ziel:** Vollständiger 4-Schritt-Picker mit Monatskalender-Datumsauswahl
und Slot-Liste. Mobile: Tagansicht + Swipe-Navigation.

**Referenz Hauptplan:** Abschnitt 4.3 (Buchungs-Picker), 4.2 (Mobile-First-Strategie).

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Application | `GetAvailableDaysQuery`, `GetAvailableTimeSlotsQuery`, `SlotOption` |
| Presentation | `GET /verfuegbare-tage`, `GET /verfuegbare-slots` |
| Frontend | Schritt-2-Monatskalender, Schritt-3-Slot-Liste, Mobile-Tagansicht, Swipe-Gesten, Touch-Selektion |

**Testbar nach Phase 8:**
- Monatskalender zeigt nur buchbare Tage
- Slot-Liste zeigt freie und belegte Zeiten
- Mobile: Tagansicht und Swipe funktionieren auf iOS/Android

---

## Phase 9 — CAPTCHA & CSV-Export

**Ziel:** Buchungsformular gegen Spam geschützt (Cloudflare Turnstile).
Buchhaltung erhält CSV-Export aller Einzelbuchungen.

**Referenz Hauptplan:** Getroffene Entscheidungen (CAPTCHA), Abschnitt 5.3 (CSV-Export).

**Was wird gebaut:**

| Schicht | Dateien |
|---------|---------|
| Infra | `TurnstileOptions`, `TurnstileCaptchaAdapter`, Port `ICaptchaPort` |
| Application | Turnstile-Prüfung in `CreateBookingUseCase` und `RegisterRenterUseCase` integrieren |
| Infra | `BookingCsvAdapter` |
| Presentation | `GET /admin/export/csv` |
| Umbraco | Turnstile Site Key in Konfigurations-Knoten ergänzen |

**Testbar nach Phase 9:**
- Buchung ohne gültigen Turnstile-Token wird abgelehnt
- CSV-Download öffnet korrekt in Excel mit UTF-8-BOM

---

## Deployment pro Phase

Jede Phase soll auf dem Feature-Branch deployt und auf
`https://app-sporthalle-sulzerallee.azurewebsites.net/` getestet werden,
bevor die nächste Phase beginnt. Azure-Env-Variablen werden phasengerecht ergänzt
(Phase 3: `Brevo__ApiKey`; Phase 9: `Turnstile__SiteKey`, `Turnstile__SecretKey`).
