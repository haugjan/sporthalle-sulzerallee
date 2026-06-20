using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Application.Reservierung;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Presentation.Reservierung.Controllers;

// ═══════════════════════════════════════════════════════════════════════════════
// REST-API für die Reservierungsverwaltung im Umbraco-Backoffice
//
// Verantwortlich: Admin-Session (feature-reservierungsverwaltung)
// Kontraktversion: 1.0 — Stand 2026-06-20
//
// Alle Endpunkte sind mit dem Umbraco-Backoffice-Auth gesichert.
// Admin-User für Audit-Log: User.Identity?.Name ?? "admin"
//
// TODO (Admin-Session):
//   - Umbraco Backoffice Section + Tree für Reservationen (IPackageManifestReader)
//   - Razor-View oder Angular-Komponente für Buchungsliste + Detailansicht
//   - IBookingSlotRepository um GetAllAsync(from, to, status?) erweitern
// ═══════════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("api/admin/reservierungen")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class ReservierungAdminApiController(
    BookingAdminService adminService,
    ConfirmBookingUseCase confirmBooking,
    RejectBookingUseCase rejectBooking,
    IBookingSlotRepository slotRepo,
    IBookingCsvPort csvExport) : ControllerBase
{
    // ── Pendente Buchungen ────────────────────────────────────────────────────

    // GET /api/admin/reservierungen/pending
    // Liefert alle Buchungen mit Status "PendingAdminApproval"
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var items = await adminService.GetPendingAsync();
        return Ok(items.Select(x => MapToDto(x.Slot, x.Member)));
    }

    // ── Alle Buchungen (gefiltert) ────────────────────────────────────────────

    // GET /api/admin/reservierungen?von=2026-01-01&bis=2026-12-31&status=Confirmed
    // TODO (Admin-Session): IBookingSlotRepository.GetAllAsync(from?, to?, status?) implementieren
    [HttpGet("")]
    public IActionResult GetAll(
        [FromQuery] string? von,
        [FromQuery] string? bis,
        [FromQuery] string? status)
    {
        return StatusCode(501, new { error = "Noch nicht implementiert — Admin-Session." });
    }

    // ── Einzel-Buchung ────────────────────────────────────────────────────────

    // GET /api/admin/reservierungen/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var slot = await slotRepo.FindByIdAsync(id);
        if (slot is null)
            return NotFound(new { error = $"Buchung {id} nicht gefunden." });
        return Ok(MapToDto(slot, null));
    }

    // ── Bestätigen ────────────────────────────────────────────────────────────

    // POST /api/admin/reservierungen/{id}/bestaetigen
    // Bestätigt die Buchung und sendet Bestätigungs-E-Mail an den Mieter
    [HttpPost("{id:int}/bestaetigen")]
    public async Task<IActionResult> Bestaetigen(int id)
    {
        try
        {
            await confirmBooking.ExecuteAsync(id, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, status = "Confirmed" });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Ablehnen ──────────────────────────────────────────────────────────────

    // POST /api/admin/reservierungen/{id}/ablehnen
    // Body: { "grund": "Halle belegt durch Schulanlass" }
    // Lehnt die Buchung ab und sendet Absage-E-Mail an den Mieter
    [HttpPost("{id:int}/ablehnen")]
    public async Task<IActionResult> Ablehnen(int id, [FromBody] AdminAblehnenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Grund))
            return BadRequest(new { error = "Ablehnungsgrund ist erforderlich." });
        try
        {
            await rejectBooking.ExecuteAsync(id, req.Grund, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, status = "Rejected" });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Abbrechen ─────────────────────────────────────────────────────────────

    // POST /api/admin/reservierungen/{id}/abbrechen
    // Bricht eine bestätigte Buchung ab (z. B. Stornierung durch Mieter)
    [HttpPost("{id:int}/abbrechen")]
    public async Task<IActionResult> Abbrechen(int id)
    {
        try
        {
            await adminService.CancelSlotAsync(id, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, status = "Cancelled" });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Preis anpassen ────────────────────────────────────────────────────────

    // POST /api/admin/reservierungen/{id}/preis
    // Body: { "preisProBlock": 45.00, "notiz": "Vereinsrabatt 10%" }
    [HttpPost("{id:int}/preis")]
    public async Task<IActionResult> PreisAnpassen(int id, [FromBody] AdminPreisRequest req)
    {
        if (req.PreisProBlock <= 0)
            return BadRequest(new { error = "Preis muss grösser als 0 sein." });
        try
        {
            await adminService.AdjustPriceAsync(id, req.PreisProBlock, req.Notiz, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, preisProBlock = req.PreisProBlock });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── CSV-Export ────────────────────────────────────────────────────────────

    // GET /api/admin/reservierungen/export?von=2026-01-01&bis=2026-12-31&nurBestaetigt=true
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string von,
        [FromQuery] string bis,
        [FromQuery] bool nurBestaetigt = true)
    {
        if (!DateOnly.TryParse(von, out var from) || !DateOnly.TryParse(bis, out var to))
            return BadRequest(new { error = "'von' und 'bis' müssen im Format YYYY-MM-DD angegeben werden." });

        var csv = await csvExport.ExportAsync(
            from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            nurBestaetigt);
        return File(csv, "text/csv", $"reservierungen-{von}-{bis}.csv");
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static object MapToDto(BookingSlot slot, HallMember? member) => new
    {
        id            = slot.Id,
        status        = slot.Status.ToString(),
        startUtc      = slot.Slot.StartUtc,
        endUtc        = slot.Slot.EndUtc,
        anlass        = slot.EventType,
        notizen       = slot.Notes,
        preisProBlock = slot.PricePerBlock,
        mitglied      = member is null ? null : new
        {
            id            = member.Id,
            email         = member.Email,
            name          = member.ContactPerson,
            rechnungsName = member.BillingName,
            strasse       = member.BillingAddress,
            plz           = member.BillingPostalCode,
            ort           = member.BillingCity,
            telefon       = member.Phone,
        }
    };
}

public sealed record AdminAblehnenRequest(string Grund);
public sealed record AdminPreisRequest(decimal PreisProBlock, string? Notiz);
