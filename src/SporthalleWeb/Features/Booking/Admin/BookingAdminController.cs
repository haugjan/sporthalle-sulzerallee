using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

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
