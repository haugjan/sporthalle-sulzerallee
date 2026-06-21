using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Presentation.Booking.Controllers;

[Route("reservierung/admin")]
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
