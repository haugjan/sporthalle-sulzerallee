using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    // ── Pending bookings ──────────────────────────────────────────────────────

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var items = await adminService.GetPendingAsync();
        return Ok(items.Select(x => MapToDto(x.Slot, x.Member)));
    }

    // ── All bookings (filtered) ───────────────────────────────────────────────

    // GET /api/admin/reservierungen?from=2026-01-01&to=2026-12-31&type=Booked
    [HttpGet("")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? type)
    {
        DateOnly? fromDate = null;
        DateOnly? toDate = null;
        SlotType? slotType = null;

        if (from is not null)
        {
            if (!DateOnly.TryParse(from, out var parsedFrom))
                return BadRequest(new { error = "'from' muss im Format YYYY-MM-DD angegeben werden." });
            fromDate = parsedFrom;
        }
        if (to is not null)
        {
            if (!DateOnly.TryParse(to, out var parsedTo))
                return BadRequest(new { error = "'to' muss im Format YYYY-MM-DD angegeben werden." });
            toDate = parsedTo;
        }
        if (type is not null)
        {
            if (!Enum.TryParse<SlotType>(type, ignoreCase: true, out var parsed))
                return BadRequest(new { error = $"Unbekannter Typ '{type}'. Erlaubt: Blocker, Reserved, Booked." });
            slotType = parsed;
        }

        var slots = await slotRepo.GetAllAsync(fromDate, toDate, slotType);
        return Ok(slots.Select(s => MapToDto(s, null)));
    }

    // ── Single booking ────────────────────────────────────────────────────────

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var slot = await slotRepo.FindByIdAsync(id);
        if (slot is null)
            return NotFound(new { error = $"Buchung {id} nicht gefunden." });
        return Ok(MapToDto(slot, null));
    }

    // ── Create slot (admin) ───────────────────────────────────────────────────

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

    // ── Confirm (Reserved → Booked) ───────────────────────────────────────────

    [HttpPost("{id:int}/bestaetigen")]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            await confirmBooking.ExecuteAsync(id, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, type = "Booked" });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Reject (deletes Reserved slot) ───────────────────────────────────────

    [HttpPost("{id:int}/ablehnen")]
    public async Task<IActionResult> Reject(int id, [FromBody] AdminRejectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { error = "Ablehnungsgrund ist erforderlich." });
        try
        {
            await rejectBooking.ExecuteAsync(id, req.Reason, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, deleted = true });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Delete (Booked or Blocker) ────────────────────────────────────────────

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await adminService.DeleteSlotAsync(id, User.Identity?.Name ?? "admin");
            return Ok(new { bookingId = id, deleted = true });
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ── Member search ─────────────────────────────────────────────────────────

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

    // ── CSV export ────────────────────────────────────────────────────────────

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
            return BadRequest(new { error = "'from' und 'to' müssen im Format YYYY-MM-DD angegeben werden." });

        var csv = await csvExport.ExportAsync(
            fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
        return File(csv, "text/csv", $"reservierungen-{from}-{to}.csv");
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
        notes    = slot.Notes,
        member   = member is null ? null : new
        {
            id               = member.Id,
            email            = member.Email,
            contactFirstName = member.ContactFirstName,
            contactLastName  = member.ContactLastName,
            name             = member.Name,
            address          = member.BillingAddress,
            addressLine2     = member.AddressLine2,
            postalCode       = member.BillingPostalCode,
            city             = member.BillingCity,
            phone            = member.Phone,
        }
    };

    // ── Dev-only seed: create recurring slots without auth ────────────────────

    [HttpPost("serientermine/seed")]
    [AllowAnonymous]
    public async Task<IActionResult> SeedRecurring(
        [FromBody] RecurringSlotSeedRequest req,
        [FromServices] CreateRecurringSlotUseCase createSerie,
        [FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var cmd = new RecurringSlotCommand(
            req.Title,
            (DayOfWeek)req.Weekday,
            TimeOnly.Parse(req.From),
            TimeOnly.Parse(req.To),
            DateOnly.Parse(req.SeriesStart),
            DateOnly.Parse(req.SeriesEnd),
            req.Color,
            req.Notes,
            IsBlocker: req.IsBlocker);

        var result = await createSerie.ExecuteAsync(cmd, "seed", skipConflicts: true);
        return Ok(result);
    }
}

public sealed record AdminRejectRequest(string Reason);
public sealed record AdminCreateSlotRequest(
    string Type, DateTime StartUtc, DateTime EndUtc,
    string Title, string? Color, string? Notes, int? MemberId);
public sealed record RecurringSlotSeedRequest(
    string Title,
    int Weekday,
    string From,
    string To,
    string SeriesStart,
    string SeriesEnd,
    string? Color,
    string? Notes,
    bool IsBlocker = false);
