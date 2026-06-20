using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Presentation.Reservierung.Controllers;

[Route("reservierung/backoffice-admin")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class ReservierungBackofficeAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
