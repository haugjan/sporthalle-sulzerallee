using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Application.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Presentation.PassivMitgliedschaft.Dtos;

namespace SporthalleWeb.Presentation.PassivMitgliedschaft.Controllers;

[ApiController]
[Route("api/passivmitglieder")]
public class PassivMitgliederController : ControllerBase
{
    private readonly RegisterMemberUseCase _registerMember;
    private readonly GetFieldStatusesQuery _getFieldStatuses;

    public PassivMitgliederController(
        RegisterMemberUseCase registerMember,
        GetFieldStatusesQuery getFieldStatuses)
    {
        _registerMember = registerMember;
        _getFieldStatuses = getFieldStatuses;
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

    // Phase 3: POST /{id}/paid, POST /{id}/notes, GET /admin/members, GET /admin/export/*
}
