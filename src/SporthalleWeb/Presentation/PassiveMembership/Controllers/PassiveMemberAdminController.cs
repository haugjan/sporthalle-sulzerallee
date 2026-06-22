using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Presentation.PassiveMembership.Controllers;

[Route("passivmitglieder/admin")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class PassiveMemberAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var adminUser = User.Identity?.Name ?? "admin";
        return View("~/Views/PassivMemberAdmin/Index.cshtml", model: adminUser);
    }
}
