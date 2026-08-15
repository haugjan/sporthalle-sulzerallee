using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Features.Email.Admin;

[Route("admin/emails")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class EmailAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index() =>
        View("~/Features/Email/Admin/Views/Admin.cshtml");
}
