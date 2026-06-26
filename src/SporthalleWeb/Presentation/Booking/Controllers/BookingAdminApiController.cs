using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SporthalleWeb.Application.Booking;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Presentation.Booking.Controllers;

[ApiController]
[Route("api/admin/reservierungen")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class BookingAdminApiController(
    BookingAdminService adminService,
    ConfirmBookingUseCase confirmBooking,
    RejectBookingUseCase rejectBooking,
    IBookingSlotRepository slotRepo,
    IBookingCsvPort csvExport,
    IMemberManagerPort memberManager) : ControllerBase
{
    // ── Reservierte Buchungen (ausstehend) ────────────────────────────────────

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var items = await adminService.GetPendingAsync();
        return Ok(items.Select(x => MapToDto(x.Slot, x.Member)));
    }

    // ── Alle Buchungen (gefiltert) ────────────────────────────────────────────

    // GET /api/admin/reservierungen?von=2026-01-01&bis=2026-12-31&type=Booked
    [HttpGet("")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? von,
        [FromQuery] string? bis,
        [FromQuery] string? type)
    {
        DateOnly? from = null;
        DateOnly? to = null;
        SlotType? slotType = null;

        if (von is not null)
        {
            if (!DateOnly.TryParse(von, out var parsedFrom))
                return BadRequest(new { error = "'von' muss im Format YYYY-MM-DD angegeben werden." });
            from = parsedFrom;
        }
        if (bis is not null)
        {
            if (!DateOnly.TryParse(bis, out var parsedTo))
                return BadRequest(new { error = "'bis' muss im Format YYYY-MM-DD angegeben werden." });
            to = parsedTo;
        }
        if (type is not null)
        {
            if (!Enum.TryParse<SlotType>(type, ignoreCase: true, out var parsed))
                return BadRequest(new { error = $"Unbekannter Typ '{type}'. Erlaubt: Blocker, Reserved, Booked." });
            slotType = parsed;
        }

        var slots = await slotRepo.GetAllAsync(from, to, slotType);
        return Ok(slots.Select(s => MapToDto(s, null)));
    }

    // ── Einzel-Buchung ────────────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var slot = await slotRepo.FindByIdAsync(id);
        if (slot is null)
            return NotFound(new { error = $"Buchung {id} nicht gefunden." });
        return Ok(MapToDto(slot, null));
    }

    // ── Erfassen (neuer Slot durch Admin) ────────────────────────────────────

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] AdminCreateSlotRequest req)
    {
        if (!Enum.TryParse<SlotType>(req.Type, ignoreCase: true, out var slotType))
            return BadRequest(new { error = $"Unbekannter Typ '{req.Type}'. Erlaubt: Blocker, Reserved, Booked." });
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { error = "Bezeichnung ist erforderlich." });
        if (slotType != SlotType.Blocker && req.MemberId is null)
            return BadRequest(new { error = "MitgliedId ist für Reserved und Booked erforderlich." });

        try
        {
            var slot = await adminService.CreateSlotAsync(
                slotType, req.StartUtc, req.EndUtc,
                req.Title, req.Color, req.Notes,
                req.MemberId, User.Identity?.Name ?? "admin");
            return Ok(MapToDto(slot, null));
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Bestätigen (Reserved → Booked) ───────────────────────────────────────

    [HttpPost("{id:int}/bestaetigen")]
    public async Task<IActionResult> Bestaetigen(int id)
    {
        try
        {
            await confirmBooking.ExecuteAsync(id, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, type = "Booked" });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Ablehnen (löscht Reserved-Slot) ──────────────────────────────────────

    [HttpPost("{id:int}/ablehnen")]
    public async Task<IActionResult> Ablehnen(int id, [FromBody] AdminRejectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Grund))
            return BadRequest(new { error = "Ablehnungsgrund ist erforderlich." });
        try
        {
            await rejectBooking.ExecuteAsync(id, req.Grund, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, deleted = true });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Löschen (Booked oder Blocker entfernen) ───────────────────────────────

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Loeschen(int id)
    {
        try
        {
            await adminService.DeleteSlotAsync(id, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, deleted = true });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Mitglieder-Suche ─────────────────────────────────────────────────────

    [HttpGet("members/search")]
    public async Task<IActionResult> SearchMembers([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(Array.Empty<object>());
        var members = await memberManager.SearchAsync(q);
        return Ok(members.Select(m => new
        {
            id = m.Id,
            email = m.Email,
            name = m.Name,
            contactFirstName = m.ContactFirstName,
            contactLastName = m.ContactLastName,
        }));
    }

    // ── CSV-Export ────────────────────────────────────────────────────────────

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string von,
        [FromQuery] string bis)
    {
        if (!DateOnly.TryParse(von, out var from) || !DateOnly.TryParse(bis, out var to))
            return BadRequest(new { error = "'von' und 'bis' müssen im Format YYYY-MM-DD angegeben werden." });

        var csv = await csvExport.ExportAsync(
            from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
        return File(csv, "text/csv", $"reservierungen-{von}-{bis}.csv");
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static object MapToDto(BookingSlot slot, HallMember? member) => new
    {
        id       = slot.Id,
        type     = slot.Type.ToString(),
        startUtc = slot.Slot.StartUtc,
        endUtc   = slot.Slot.EndUtc,
        title    = slot.Title,
        color    = slot.Color,
        notizen  = slot.Notes,
        mitglied = member is null ? null : new
        {
            id               = member.Id,
            email            = member.Email,
            contactFirstName = member.ContactFirstName,
            contactLastName  = member.ContactLastName,
            name             = member.Name,
            strasse          = member.BillingAddress,
            adresszusatz     = member.AddressLine2,
            plz              = member.BillingPostalCode,
            ort              = member.BillingCity,
            telefon          = member.Phone,
        }
    };

    // ── Dev-only Seed: Serientermine ohne Auth anlegen ───────────────────────

    [HttpPost("serientermine/seed")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedSerie(
        [FromBody] SerieTerminSeedRequest req,
        [FromServices] CreateRecurringSlotUseCase createSerie,
        [FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var cmd = new RecurringSlotCommand(
            req.Title,
            (DayOfWeek)req.Wochentag,
            TimeOnly.Parse(req.Von),
            TimeOnly.Parse(req.Bis),
            DateOnly.Parse(req.SerieVon),
            DateOnly.Parse(req.SerieBis),
            req.Color,
            req.Notes,
            IsBlocker: req.IsBlocker);

        var result = await createSerie.ExecuteAsync(cmd, "seed", skipConflicts: true);
        return Ok(result);
    }
}

public sealed record AdminRejectRequest(string Grund);
public sealed record AdminCreateSlotRequest(
    string Type, DateTime StartUtc, DateTime EndUtc,
    string Title, string? Color, string? Notes, int? MemberId);
public sealed record SerieTerminSeedRequest(
    string Title,
    int Wochentag,
    string Von,
    string Bis,
    string SerieVon,
    string SerieBis,
    string? Color,
    string? Notes,
    bool IsBlocker = false);
