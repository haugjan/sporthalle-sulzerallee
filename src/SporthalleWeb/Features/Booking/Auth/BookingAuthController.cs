using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Features.Booking.Auth;

[Route("reservierung")]
public sealed class BookingAuthController(
    ValidateMagicLink validateMagicLink,
    SendMagicLink sendMagicLink,
    LoginWithPassword loginWithPassword,
    RegisterRenter registerRenter) : Controller
{
    // GET /reservierung/auth/validate?token=... — magic link landing page
    [HttpGet("auth/validate")]
    public async Task<IActionResult> ValidateMagicLink([FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Redirect("/reservierung?error=missing-token");
        try
        {
            await validateMagicLink.ExecuteAsync(token);
            return Redirect("/reservierung?session=confirmed");
        }
        catch (DomainException)
        {
            return Redirect("/reservierung?error=invalid-token");
        }
    }

    // GET /reservierung/anmelden
    [HttpGet("anmelden")]
    public IActionResult Anmelden([FromQuery] string? error, [FromQuery] string? info)
    {
        ViewBag.Error = error;
        ViewBag.Info = info;
        ViewBag.Title = "Anmelden – Sporthalle Sulzerallee";
        ViewBag.BodyClass = "";
        return View();
    }

    // POST /reservierung/anmelden/password
    [HttpPost("anmelden/password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithPassword([FromForm] string email, [FromForm] string password)
    {
        try
        {
            await loginWithPassword.ExecuteAsync(email, password);
            return Redirect("/reservierung");
        }
        catch (DomainException ex)
        {
            return Redirect($"/reservierung/anmelden?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // POST /reservierung/anmelden/magic-link
    [HttpPost("anmelden/magic-link")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestMagicLink([FromForm] string email)
    {
        try
        {
            var sent = await sendMagicLink.ExecuteAsync(
                email, HttpContext.Connection.RemoteIpAddress?.ToString());
            var info = sent
                ? "Anmelde-Link wurde gesendet. Bitte prüfe deine E-Mails."
                : "Falls die Adresse bekannt ist, haben wir dir einen Link gesendet.";
            return Redirect($"/reservierung/anmelden?info={Uri.EscapeDataString(info)}");
        }
        catch (DomainException ex)
        {
            return Redirect($"/reservierung/anmelden?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // GET /reservierung/registrieren
    [HttpGet("registrieren")]
    public IActionResult Registrieren([FromQuery] string? error)
    {
        ViewBag.Error = error;
        ViewBag.Title = "Registrieren – Sporthalle Sulzerallee";
        ViewBag.BodyClass = "";
        return View();
    }

    // POST /reservierung/registrieren
    [HttpPost("registrieren")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(
        [FromForm] string email,
        [FromForm] string? name,
        [FromForm] string contactFirstName,
        [FromForm] string contactLastName,
        [FromForm] string renterType,
        [FromForm] string billingAddress,
        [FromForm] string? addressLine2,
        [FromForm] string billingPostalCode,
        [FromForm] string billingCity,
        [FromForm] string? phone,
        [FromForm] string? password,
        [FromForm(Name = "cf-turnstile-response")] string? captchaToken)
    {
        try
        {
            var cmd = new RegisterRenterCommand(
                Email: email,
                RenterType: new RenterType(renterType),
                Name: name,
                ContactFirstName: contactFirstName,
                ContactLastName: contactLastName,
                BillingAddress: billingAddress,
                AddressLine2: addressLine2,
                BillingPostalCode: billingPostalCode,
                BillingCity: billingCity,
                BillingCountry: "Schweiz",
                Phone: phone,
                HasKey: false,
                Password: string.IsNullOrWhiteSpace(password) ? null : password);
            await registerRenter.ExecuteAsync(
                cmd, captchaToken, HttpContext.Connection.RemoteIpAddress?.ToString());
            return Redirect("/reservierung/anmelden?info="
                + Uri.EscapeDataString("Registrierung erfolgreich! Anmelde-Link wurde per E-Mail gesendet."));
        }
        catch (DomainException ex)
        {
            return Redirect($"/reservierung/registrieren?error={Uri.EscapeDataString(ex.Message)}");
        }
    }
}
