using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

[Route("passivmitglieder/zahlung")]
public sealed class PaymentResultController : Controller
{
    [HttpGet("danke")]
    public IActionResult ThankYou()
        => View("~/Features/PassiveMembership/Registration/Views/PaymentThankYou.cshtml");

    [HttpGet("abbruch")]
    public IActionResult Cancelled()
        => View("~/Features/PassiveMembership/Registration/Views/PaymentCancelled.cshtml");
}
