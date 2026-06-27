using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

[ApiController]
[Route("api/passivmitglieder/admin")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public class PassiveMemberAdminController(PassiveMemberAdmin adminService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingMembers()
    {
        var members = await adminService.GetPendingAsync();
        return Ok(members.Select(m => new
        {
            m.Id,
            FieldNumber = m.FieldNumber.Value,
            VipLabel = VipField.GetLabel(m.FieldNumber.Value),
            Level = m.Level.DisplayName,
            LevelKey = m.Level.Key,
            m.FirstName,
            m.LastName,
            Email = m.Email.Value,
            m.AddressLine,
            m.PostalCode.Value,
            m.City,
            CreatedAt = m.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        }));
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetConfirmedMembers()
    {
        var members = await adminService.GetConfirmedAsync();
        return Ok(members.Select(m => new
        {
            m.Id,
            FieldNumber = m.FieldNumber.Value,
            VipLabel = VipField.GetLabel(m.FieldNumber.Value),
            Level = m.Level.DisplayName,
            LevelKey = m.Level.Key,
            m.FirstName,
            m.LastName,
            Email = m.Email.Value,
            m.AddressLine,
            m.PostalCode.Value,
            m.City,
            CreatedAt = m.CreatedAt.ToString("dd.MM.yyyy"),
            PaidAt = m.PaidAt?.ToString("dd.MM.yyyy"),
            m.PaidBy,
            m.ExportedToAccounting,
            m.Notes
        }));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmMember(int id, [FromBody] ConfirmMemberRequest req)
    {
        var confirmedBy = User.Identity?.Name ?? "admin";
        try
        {
            await adminService.ConfirmAsync(id, req.IsPaid, confirmedBy);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDeleteMember(int id)
    {
        try
        {
            await adminService.SoftDeleteAsync(id);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/paid")]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        var paidBy = User.Identity?.Name ?? "admin";
        try
        {
            await adminService.MarkAsPaidAsync(id, paidBy);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/unpaid")]
    public async Task<IActionResult> MarkAsUnpaid(int id)
    {
        try
        {
            await adminService.MarkAsUnpaidAsync(id);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/accounting/mark")]
    public async Task<IActionResult> MarkAccounting(int id, [FromBody] MarkAccountingRequest req)
    {
        try
        {
            await adminService.MarkAsExportedToAccountingAsync(id, req.By);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/accounting/unmark")]
    public async Task<IActionResult> UnmarkAccounting(int id)
    {
        try
        {
            await adminService.UnmarkAsExportedToAccountingAsync(id);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, [FromBody] UpdateNotesRequest req)
    {
        try
        {
            await adminService.UpdateNotesAsync(id, req.Notes);
            return Ok();
        }
        catch (MemberNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel()
    {
        var bytes = await adminService.ExportExcelAsync();
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "passivmitglieder.xlsx");
    }

    [HttpGet("export/abaninja")]
    public async Task<IActionResult> ExportAbaninja()
    {
        var bytes = await adminService.ExportAbaninjaAsync();
        return File(bytes, "text/csv; charset=utf-8", "passivmitglieder-abaninja.csv");
    }
}

public record UpdateNotesRequest(string? Notes);
public record ConfirmMemberRequest(bool IsPaid);
public record MarkAccountingRequest(string By);
