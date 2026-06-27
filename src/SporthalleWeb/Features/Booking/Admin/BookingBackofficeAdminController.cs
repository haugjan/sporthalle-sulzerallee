using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

[Route("admin/reservierungen/backoffice")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class BookingBackofficeAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return View();
    }
}
