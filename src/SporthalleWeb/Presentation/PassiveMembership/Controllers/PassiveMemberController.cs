using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Application.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;
using SporthalleWeb.Presentation.PassiveMembership.Dtos;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Presentation.PassiveMembership.Controllers;

[ApiController]
[Route("api/passivmitglieder")]
public class PassiveMemberController : ControllerBase
{
    private readonly RegisterMemberUseCase _registerMember;
    private readonly GetFieldStatusesQuery _getFieldStatuses;
    private readonly ICaptchaPort _captcha;
    private readonly AdminService _adminService;

    public PassiveMemberController(
        RegisterMemberUseCase registerMember,
        GetFieldStatusesQuery getFieldStatuses,
        ICaptchaPort captcha,
        AdminService adminService)
    {
        _registerMember = registerMember;
        _getFieldStatuses = getFieldStatuses;
        _captcha = captcha;
        _adminService = adminService;
    }

    [HttpGet("felder")]
    public async Task<IActionResult> GetFelder()
    {
        var result = await _getFieldStatuses.ExecuteAsync();
        var items = result.OccupiedFields
            .Select(f => new FieldStatusItem(f.FieldNumber, f.DisplayName, f.VipLabel))
            .ToList();
        return Ok(new FieldStatusResponse(items, result.TotalFields, items.Count));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterMemberRequest req)
    {
        if (!req.Consent)
            return BadRequest(new { error = "consent_required" });

        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName) ||
            string.IsNullOrWhiteSpace(req.AddressLine) || string.IsNullOrWhiteSpace(req.PostalCode) ||
            string.IsNullOrWhiteSpace(req.City) || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { error = "missing_fields" });

        if (string.IsNullOrWhiteSpace(req.CaptchaToken))
            return BadRequest(new { error = "captcha_required" });

        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        if (!await _captcha.VerifyAsync(req.CaptchaToken, remoteIp))
            return BadRequest(new { error = "captcha_failed" });

        try
        {
            var cmd = new RegisterMemberCommand(
                req.FieldNumber, req.FirstName, req.LastName,
                req.AddressLine, req.PostalCode, req.City,
                req.Email, req.LevelKey,
                req.ShowNameOnFloor, req.DisplayName, req.Consent);

            var member = await _registerMember.ExecuteAsync(cmd);
            return Ok(new { memberId = member.Id });
        }
        catch (FieldAlreadyTakenException ex)
        {
            return Conflict(new { error = "field_taken", message = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = "domain_error", message = ex.Message });
        }
    }

    // Admin endpoints

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("admin/pending")]
    public async Task<IActionResult> GetPendingMembers()
    {
        var members = await _adminService.GetPendingAsync();
        return Ok(members.Select(m => new
        {
            m.Id,
            FieldNumber = m.FieldNumber.Value,
            VipLabel = VipField.GetLabel(m.FieldNumber.Value),
            Level = m.Level.DisplayName,
            LevelKey = m.Level.Key,
            YearlyFee = m.Level.YearlyFee,
            m.FirstName,
            m.LastName,
            Email = m.Email.Value,
            m.AddressLine,
            m.PostalCode,
            m.City,
            CreatedAt = m.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        }));
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("admin/members")]
    public async Task<IActionResult> GetConfirmedMembers()
    {
        var members = await _adminService.GetConfirmedAsync();
        return Ok(members.Select(m => new
        {
            m.Id,
            FieldNumber = m.FieldNumber.Value,
            VipLabel = VipField.GetLabel(m.FieldNumber.Value),
            Level = m.Level.DisplayName,
            LevelKey = m.Level.Key,
            YearlyFee = m.Level.YearlyFee,
            m.FirstName,
            m.LastName,
            Email = m.Email.Value,
            m.AddressLine,
            m.PostalCode,
            m.City,
            CreatedAt = m.CreatedAt.ToString("dd.MM.yyyy"),
            PaidAt = m.PaidAt?.ToString("dd.MM.yyyy"),
            m.PaidBy,
            m.ExportedToAccounting,
            m.Notes
        }));
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpPost("admin/{id:int}/confirm")]
    public async Task<IActionResult> ConfirmMember(int id, [FromBody] ConfirmMemberRequest req)
    {
        var confirmedBy = User.Identity?.Name ?? "admin";
        try
        {
            await _adminService.ConfirmAsync(id, req.IsPaid, confirmedBy);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpDelete("admin/{id:int}")]
    public async Task<IActionResult> SoftDeleteMember(int id)
    {
        try
        {
            await _adminService.SoftDeleteAsync(id);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpPost("admin/{id:int}/paid")]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        var paidBy = User.Identity?.Name ?? "admin";
        try
        {
            await _adminService.MarkAsPaidAsync(id, paidBy);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpPost("admin/{id:int}/unpaid")]
    public async Task<IActionResult> MarkAsUnpaid(int id)
    {
        try
        {
            await _adminService.MarkAsUnpaidAsync(id);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpPost("admin/{id:int}/accounting/mark")]
    public async Task<IActionResult> MarkAccounting(int id, [FromBody] MarkAccountingRequest req)
    {
        try
        {
            await _adminService.MarkAsExportedToAccountingAsync(id, req.By);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpPost("admin/{id:int}/accounting/unmark")]
    public async Task<IActionResult> UnmarkAccounting(int id)
    {
        try
        {
            await _adminService.UnmarkAsExportedToAccountingAsync(id);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpPost("admin/{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, [FromBody] UpdateNotesRequest req)
    {
        try
        {
            await _adminService.UpdateNotesAsync(id, req.Notes);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("admin/export/excel")]
    public async Task<IActionResult> ExportExcel()
    {
        var bytes = await _adminService.ExportExcelAsync();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "passivmitglieder.xlsx");
    }

    [Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
    [HttpGet("admin/export/abaninja")]
    public async Task<IActionResult> ExportAbaninja()
    {
        var bytes = await _adminService.ExportAbaninjaAsync();
        return File(bytes, "text/csv; charset=utf-8", "passivmitglieder-abaninja.csv");
    }
}

public record UpdateNotesRequest(string? Notes);
public record ConfirmMemberRequest(bool IsPaid);
public record MarkAccountingRequest(string By);
