using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SporthalleWeb.Presentation.PassiveMembership.Controllers;

[Route("passivmitglieder/hallenboden")]
public sealed class FloorPlanController(IConfiguration config) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var siteKey = config["Turnstile:SiteKey"] ?? "1x00000000000000000000AA";
        return View("~/Views/FloorPlan/Index.cshtml", siteKey);
    }
}
