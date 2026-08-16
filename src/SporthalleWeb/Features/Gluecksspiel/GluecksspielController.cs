using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Features.PassiveMembership.Registration;

namespace SporthalleWeb.Features.Gluecksspiel;

[Route("glueck")]
public class GluecksspielController(IPassiveMembers members, IConfiguration config) : Controller
{
    private string? ConfigSecret => config["Gluecksspiel:Secret"];

    [HttpGet("{secret}")]
    public IActionResult Index(string secret)
    {
        if (string.IsNullOrEmpty(ConfigSecret) || !secret.Equals(ConfigSecret, StringComparison.Ordinal))
            return NotFound();
        ViewBag.Secret = secret;
        return View("~/Features/Gluecksspiel/Views/Index.cshtml");
    }

    [HttpGet("{secret}/draw")]
    public async Task<IActionResult> Draw(string secret)
    {
        if (string.IsNullOrEmpty(ConfigSecret) || !secret.Equals(ConfigSecret, StringComparison.Ordinal))
            return NotFound();

        var confirmed = await members.GetConfirmedAsync();
        var paid = confirmed.Where(m => m.PaidAt != null).ToList();

        if (paid.Count == 0)
            return BadRequest(new { error = "Keine bezahlten Mitglieder gefunden." });

        var winner = paid[Random.Shared.Next(paid.Count)];
        var pool = paid.Select(m => m.FirstName).ToList();

        return Json(new
        {
            winner = new { firstName = winner.FirstName, fieldNumber = winner.FieldNumber.Value },
            pool
        });
    }
}
