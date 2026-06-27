using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

[Route("admin/passivmitglieder")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class PassiveMemberAdminViewController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var adminUser = User.Identity?.Name ?? "admin";
        return View("~/Features/PassiveMembership/MemberAdmin/Views/Admin.cshtml", model: adminUser);
    }
}
