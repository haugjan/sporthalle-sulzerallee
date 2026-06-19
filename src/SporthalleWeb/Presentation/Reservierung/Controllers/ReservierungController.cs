using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Application.Reservierung;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Presentation.Reservierung.Dtos;

namespace SporthalleWeb.Presentation.Reservierung.Controllers;

[ApiController]
[Route("api/reservierung")]
public sealed class ReservierungController(
    GetWeekSlotsQuery weekSlotsQuery,
    GetAvailableDaysQuery availableDaysQuery,
    GetAvailableTimeSlotsQuery availableTimeSlotsQuery,
    SendMagicLinkUseCase sendMagicLink,
    ValidateMagicLinkUseCase validateMagicLink,
    RegisterRenterUseCase registerRenter,
    LoginWithPasswordUseCase loginWithPassword,
    SetPasswordUseCase setPassword,
    RequestPasswordResetUseCase requestPasswordReset,
    ResetPasswordUseCase resetPassword,
    CreateBookingUseCase createBooking,
    ConfirmBookingUseCase confirmBooking,
    RejectBookingUseCase rejectBooking,
    CreateRecurringRuleUseCase createRecurringRule,
    BookingAdminService adminService,
    IBookingSlotRepository slotRepo,
    IBookingCsvPort csvExport,
    IMemberManagerPort memberManager) : ControllerBase
{
    // ── Calendar / week view ──────────────────────────────────────────────────

    [HttpGet("wochen-slots")]
    public async Task<IActionResult> GetWochenSlots([FromQuery] string von)
    {
        if (!DateOnly.TryParseExact(von, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var date))
            return BadRequest("Parameter 'von' muss im Format YYYY-MM-DD angegeben werden.");

        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-daysFromMonday);
        return Ok(await weekSlotsQuery.ExecuteAsync(monday));
    }

    [HttpGet("verfuegbare-tage")]
    public async Task<IActionResult> GetVerfuegbareTage(
        [FromQuery] string monat, [FromQuery] int dauern = 60)
    {
        return Ok(await availableDaysQuery.GetAsync(monat, dauern));
    }

    [HttpGet("verfuegbare-slots")]
    public async Task<IActionResult> GetVerfuegbareSlots(
        [FromQuery] string datum, [FromQuery] int dauern = 60)
    {
        return Ok(await availableTimeSlotsQuery.GetAsync(datum, dauern));
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [HttpPost("auth/magic-link")]
    public async Task<IActionResult> SendMagicLink([FromBody] SendMagicLinkRequest req)
    {
        try
        {
            await sendMagicLink.ExecuteAsync(req.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Ok(new { message = "Falls die E-Mail-Adresse bekannt ist, erhalten Sie einen Anmelde-Link." });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("auth/validate")]
    public async Task<IActionResult> ValidateMagicLink([FromBody] ValidateMagicLinkRequest req)
    {
        try
        {
            var member = await validateMagicLink.ExecuteAsync(req.Token);
            return Ok(HallMemberDto.From(member));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("auth/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRenterRequest req)
    {
        try
        {
            var cmd = new RegisterRenterCommand(
                Email: req.Email,
                ContactPerson: req.ContactPerson,
                RenterType: new RenterType(req.RenterType),
                BillingName: req.BillingName,
                BillingAddress: req.BillingAddress,
                BillingPostalCode: req.BillingPostalCode,
                BillingCity: req.BillingCity,
                BillingCountry: req.BillingCountry,
                Phone: req.Phone,
                HasKey: req.HasKey,
                Password: req.Password);

            await registerRenter.ExecuteAsync(
                cmd, req.CaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { message = "Registrierung erfolgreich. Sie erhalten eine Bestätigungs-E-Mail mit Anmelde-Link." });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        try
        {
            var member = await loginWithPassword.ExecuteAsync(req.Email, req.Password);
            return Ok(HallMemberDto.From(member));
        }
        catch (DomainException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpPost("auth/logout")]
    [Authorize]
    public IActionResult Logout() => Ok();

    [HttpPost("auth/password")]
    [Authorize]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
    {
        var memberId = GetMemberId();
        if (memberId is null) return Unauthorized();
        try
        {
            await setPassword.ExecuteAsync(memberId.Value, req.NewPassword);
            return Ok();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("auth/request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] SendMagicLinkRequest req)
    {
        await requestPasswordReset.ExecuteAsync(req.Email);
        return Ok(new { message = "Falls die E-Mail-Adresse bekannt ist, erhalten Sie einen Reset-Link." });
    }

    [HttpPost("auth/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        try
        {
            await resetPassword.ExecuteAsync(req.MemberId, req.Token, req.NewPassword);
            return Ok();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ── Current member ────────────────────────────────────────────────────────

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentMember()
    {
        var memberId = GetMemberId();
        if (memberId is null) return Unauthorized();
        var member = await memberManager.FindByIdAsync(memberId.Value);
        if (member is null) return Unauthorized();
        return Ok(HallMemberDto.From(member));
    }

    // ── Bookings (member) ─────────────────────────────────────────────────────

    [HttpGet("meine-buchungen")]
    [Authorize]
    public async Task<IActionResult> GetMeineBuchungen()
    {
        var memberId = GetMemberId();
        if (memberId is null) return Unauthorized();
        var slots = await slotRepo.GetForMemberAsync(memberId.Value);
        return Ok(slots.Select(BookingSlotDto.From));
    }

    [HttpPost("buchungen")]
    [Authorize]
    public async Task<IActionResult> CreateBuchung([FromBody] CreateBookingRequest req)
    {
        var memberId = GetMemberId();
        if (memberId is null) return Unauthorized();
        try
        {
            var slot = await createBooking.ExecuteAsync(new CreateBookingCommand(
                memberId.Value, req.StartUtc, req.EndUtc, req.EventType, req.Notes));
            return Ok(BookingSlotDto.From(slot));
        }
        catch (SlotConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

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
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("admin/buchungen/{id:int}/cancel")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await adminService.CancelSlotAsync(id, User.Identity?.Name ?? "admin");
            return Ok();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("admin/buchungen/{id:int}/preis")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AdjustPreis(int id, [FromBody] AdjustPriceRequest req)
    {
        try
        {
            await adminService.AdjustPriceAsync(id, req.NewPricePerBlock, req.Note, User.Identity?.Name ?? "admin");
            return Ok();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("admin/recurring-rules")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CreateRecurringRule([FromBody] CreateRecurringRuleRequest req)
    {
        try
        {
            if (!DateOnly.TryParseExact(req.ValidFrom, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var validFrom) ||
                !DateOnly.TryParseExact(req.ValidUntil, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var validUntil))
                return BadRequest(new { error = "ValidFrom und ValidUntil müssen im Format YYYY-MM-DD angegeben werden." });

            var cmd = new CreateRecurringRuleCommand
            {
                MemberId = req.MemberId,
                Description = req.Description,
                DayOfWeek = req.DayOfWeek,
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                ValidFrom = validFrom,
                ValidUntil = validUntil,
                IntervalWeeks = req.IntervalWeeks,
                ExcludeSchoolHolidays = req.ExcludeSchoolHolidays,
                Color = req.Color,
                Notes = req.Notes
            };

            var rule = await createRecurringRule.ExecuteAsync(cmd, User.Identity?.Name ?? "admin");
            return Ok(new { rule.Id, rule.Description });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("admin/schulferien")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetSchulferien() =>
        Ok(await adminService.GetSchoolHolidaysAsync());

    [HttpPost("admin/schulferien")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AddSchulferien([FromBody] SchulferienRequest req)
    {
        if (!DateOnly.TryParseExact(req.Von, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var von) ||
            !DateOnly.TryParseExact(req.Bis, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var bis))
            return BadRequest(new { error = "Von und Bis müssen im Format YYYY-MM-DD angegeben werden." });

        var holiday = await adminService.AddHolidayAsync(req.Name, von, bis);
        return Ok(holiday);
    }

    [HttpDelete("admin/schulferien/{id:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteSchulferien(int id)
    {
        await adminService.DeleteHolidayAsync(id);
        return NoContent();
    }

    [HttpGet("admin/export")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string von, [FromQuery] string bis,
        [FromQuery] bool nurBestaetigt = false)
    {
        if (!DateOnly.TryParseExact(von, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var fromDate) ||
            !DateOnly.TryParseExact(bis, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var toDate))
            return BadRequest(new { error = "Von und Bis müssen im Format YYYY-MM-DD angegeben werden." });

        var fromUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var toUtc = toDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();
        var csv = await csvExport.ExportAsync(fromUtc, toUtc, nurBestaetigt);

        return File(csv, "text/csv; charset=utf-8",
            $"buchungen_{von}_{bis}.csv");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int? GetMemberId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record SchulferienRequest(string Name, string Von, string Bis);
