using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

/// <summary>
/// Landing pages RaiseNow redirects the customer to after the TWINT payment.
/// Configured in the RaiseNow touchpoint as the success / cancel redirect URLs.
/// These pages are purely informational: they never mark a membership as paid,
/// because a browser redirect is not a trustworthy payment proof. The authoritative
/// "paid" status is set in the backoffice, reconciled via the field number that
/// travels along as a RaiseNow custom parameter.
/// </summary>
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
