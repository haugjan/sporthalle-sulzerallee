using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Features.Booking.Admin;

[Route("admin/reservierungen")]
[Authorize(Roles = "admin")]
public sealed class BookingAdminController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.Title = "Reservationsverwaltung – Sporthalle Sulzerallee";
        ViewBag.BodyClass = "admin-page";
        return View();
    }
}
