using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Presentation.Reservierung.Controllers;

[Route("reservierung/admin")]
[Authorize(Roles = "admin")]
public sealed class ReservierungAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.Title = "Reservierungsverwaltung – Sporthalle Sulzerallee";
        ViewBag.BodyClass = "admin-page";
        return View();
    }
}
