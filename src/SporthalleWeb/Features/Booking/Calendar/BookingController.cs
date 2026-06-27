using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Admin;
using SporthalleWeb.Features.Booking.Dtos;
using SporthalleWeb.Features.Booking.Ports;
using SporthalleWeb.Features.Booking.Requests;

namespace SporthalleWeb.Features.Booking.Calendar;

[ApiController]
[Route("api/reservierung")]
public sealed class BookingController(
    GetWeekSlots weekSlotsQuery,
    GetAvailableDays availableDaysQuery,
    GetAvailableTimeSlots availableTimeSlotsQuery,
    CreateBooking createBooking,
    ConfirmBooking confirmBooking,
    RejectBooking rejectBooking,
    BookingAdminService adminService,
    IBookingCsv csvExport,
    IHallMembers memberManager,
    IHallConfiguration hallConfig,
    ICaptcha captcha) : ControllerBase
{
    // ── Configuration ─────────────────────────────────────────────────────────

    [HttpGet("konfiguration")]
    public async Task<IActionResult> GetConfiguration()
    {
        var openingHourStart = await hallConfig.GetOpeningHourStartAsync();
        var openingHourEnd = await hallConfig.GetOpeningHourEndAsync();
        var durations = await hallConfig.GetBookableDurationsAsync();
        var priceText = await hallConfig.GetPriceTextAsync();
        var noticeDays = await hallConfig.GetShortNoticeDaysAsync();
        var cutoffDate = await hallConfig.GetBookingCutoffDateAsync();
        return Ok(new
        {
            openingHourStart,
            openingHourEnd,
            bookableDurations = durations,
            priceText,
            noticeDays,
            bookingCutoffDate = cutoffDate?.ToString("yyyy-MM-dd")
        });
    }

    // ── Guest booking ─────────────────────────────────────────────────────────

    [HttpPost("gast-buchung")]
    public async Task<IActionResult> GuestBooking([FromBody] GuestBookingRequest req)
    {
        if (!await captcha.VerifyAsync(req.CaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString()))
            return BadRequest(new { error = "CAPTCHA-Überprüfung fehlgeschlagen. Bitte die Seite neu laden." });

        if (string.IsNullOrWhiteSpace(req.GuestEmail) || !req.GuestEmail.Contains('@'))
            return BadRequest(new { error = "Ungültige E-Mail-Adresse." });
        if (req.EndUtc <= req.StartUtc || (req.EndUtc - req.StartUtc).TotalMinutes < 60)
            return BadRequest(new { error = "Mindestdauer ist 60 Minuten." });

        var zurich = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(req.EndUtc, zurich);
        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(req.StartUtc, zurich);
        var closingHour = await hallConfig.GetOpeningHourEndAsync();
        var openingHour = await hallConfig.GetOpeningHourStartAsync();
        if (endLocal.Hour > closingHour || (endLocal.Hour == closingHour && endLocal.Minute > 0))
            return BadRequest(new { error = $"Buchungsende darf nicht nach {closingHour}:00 Uhr liegen." });
        if (startLocal.Hour < openingHour)
            return BadRequest(new { error = $"Buchungsstart darf nicht vor {openingHour}:00 Uhr liegen." });

        var cutoffDate = await hallConfig.GetBookingCutoffDateAsync();
        if (cutoffDate.HasValue && DateOnly.FromDateTime(startLocal) > cutoffDate.Value)
            return BadRequest(new { error = $"Online-Buchungen sind nur bis {cutoffDate.Value:dd.MM.yyyy} möglich." });

        try
        {
            var existing = await memberManager.FindByEmailAsync(req.GuestEmail.Trim());
            int memberId;
            if (existing is not null)
            {
                await memberManager.UpdateProfileAsync(
                    existing.Id,
                    req.Name?.Trim(),
                    req.ContactFirstName.Trim(),
                    req.ContactLastName.Trim(),
                    req.BillingAddress.Trim(),
                    req.AddressLine2?.Trim(),
                    req.BillingPostalCode.Trim(),
                    req.BillingCity.Trim(),
                    string.IsNullOrWhiteSpace(req.GuestPhone) ? null : req.GuestPhone.Trim());
                memberId = existing.Id;
            }
            else
            {
                var cmd = new RegisterRenterCommand(
                    Email: req.GuestEmail.Trim(),
                    RenterType: new RenterType(req.RenterType),
                    Name: req.Name?.Trim(),
                    ContactFirstName: req.ContactFirstName.Trim(),
                    ContactLastName: req.ContactLastName.Trim(),
                    BillingAddress: req.BillingAddress.Trim(),
                    AddressLine2: req.AddressLine2?.Trim(),
                    BillingPostalCode: req.BillingPostalCode.Trim(),
                    BillingCity: req.BillingCity.Trim(),
                    BillingCountry: "Schweiz",
                    Phone: string.IsNullOrWhiteSpace(req.GuestPhone) ? null : req.GuestPhone.Trim(),
                    HasKey: false);
                var member = await memberManager.CreateAsync(cmd);
                memberId = member.Id;
            }

            var booking = await createBooking.ExecuteAsync(new CreateBookingCommand(
                memberId, req.StartUtc, req.EndUtc, req.Title, req.Notes));

            return Ok(new { bookingId = booking.Id, memberEmail = req.GuestEmail.Trim() });
        }
        catch (SlotConflictException)
        {
            return Conflict(new { error = "Dieser Zeitslot ist leider nicht mehr verfügbar. Bitte wähle einen anderen Termin." });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Calendar / week view ──────────────────────────────────────────────────

    [HttpGet("wochen-slots")]
    public async Task<IActionResult> GetWeekSlots([FromQuery] DateOnly from)
    {
        var daysFromMonday = ((int)from.DayOfWeek + 6) % 7;
        var monday = from.AddDays(-daysFromMonday);
        return Ok(await weekSlotsQuery.ExecuteAsync(monday));
    }

    [HttpGet("verfuegbare-tage")]
    public async Task<IActionResult> GetAvailableDays(
        [FromQuery] string month, [FromQuery] int duration = 60)
        => Ok(await availableDaysQuery.GetAsync(month, duration));

    [HttpGet("verfuegbare-slots")]
    public async Task<IActionResult> GetAvailableTimeSlots(
        [FromQuery] DateOnly date, [FromQuery] int duration = 60)
        => Ok(await availableTimeSlotsQuery.GetAsync(date, duration));

    // ── Admin endpoints ───────────────────────────────────────────────────────

    [HttpGet("admin/pending")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetPending()
    {
        var items = await adminService.GetPendingAsync();
        return Ok(items.Select(i => new AdminBookingResponse(
            BookingSlotDto.From(i.Slot),
            i.Member is not null ? HallMemberDto.From(i.Member) : null)));
    }

    [HttpPost("admin/buchungen/{id:int}/confirm")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            await confirmBooking.ExecuteAsync(id, User.Identity?.Name ?? "admin");
            return Ok();
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("admin/buchungen/{id:int}/reject")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] string reason)
    {
        try
        {
            await rejectBooking.ExecuteAsync(id, reason, User.Identity?.Name ?? "admin");
            return Ok();
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("admin/buchungen/{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await adminService.DeleteSlotAsync(id, User.Identity?.Name ?? "admin");
            return NoContent();
        }
        catch (DomainException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("admin/export")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ExportCsv([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();
        var csv = await csvExport.ExportAsync(fromUtc, toUtc);

        return File(csv, "text/csv; charset=utf-8", $"buchungen_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.csv");
    }
}

public sealed record GuestBookingRequest(
    string ContactFirstName,
    string ContactLastName,
    string? Name,
    string GuestEmail,
    string? GuestPhone,
    string RenterType,
    string BillingAddress,
    string? AddressLine2,
    string BillingPostalCode,
    string BillingCity,
    DateTime StartUtc,
    DateTime EndUtc,
    string Title,
    string? Notes,
    string? CaptchaToken);
