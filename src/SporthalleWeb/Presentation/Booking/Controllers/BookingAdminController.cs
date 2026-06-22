using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Presentation.Booking.Controllers;

[Route("admin/reservierungen")]
[Authorize(Roles = "admin")]
public sealed class BookingAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.Title = "Reservierungsverwaltung – Sporthalle Sulzerallee";
        ViewBag.BodyClass = "admin-page";
        return View();
    }
}
