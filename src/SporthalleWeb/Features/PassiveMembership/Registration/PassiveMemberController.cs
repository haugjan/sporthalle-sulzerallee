using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

[ApiController]
[Route("api/passivmitglieder")]
public class PassiveMemberController(
    RegisterMember registerMember,
    GetFieldStatuses getFieldStatuses,
    ICaptcha captcha)
    : ControllerBase
{
    [HttpGet("felder")]
    public async Task<IActionResult> GetFelder()
    {
        var result = await getFieldStatuses.ExecuteAsync();
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
        if (!await captcha.VerifyAsync(req.CaptchaToken, remoteIp))
            return BadRequest(new { error = "captcha_failed" });

        try
        {
            var cmd = new RegisterMemberCommand(
                req.FieldNumber, req.FirstName, req.LastName,
                req.AddressLine, req.AddressLine2, req.PostalCode, req.City,
                req.Phone, req.Email, req.LevelKey,
                req.ShowNameOnFloor, req.DisplayName, req.Consent);

            var member = await registerMember.ExecuteAsync(cmd);
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
}
