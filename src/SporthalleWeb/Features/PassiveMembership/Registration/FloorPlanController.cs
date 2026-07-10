using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

[Route("passivmitglieder/hallenboden")]
public sealed class FloorPlanController(IConfiguration config, IFloorPlanSettings settings) : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        var siteKey = config["Turnstile:SiteKey"] is { Length: > 0 } k ? k : "1x00000000000000000000AA";
        return View("~/Features/PassiveMembership/Registration/Views/FloorPlan.cshtml", siteKey);
    }

    [HttpGet("config")]
    public async Task<IActionResult> Config()
    {
        var s = await settings.GetAsync();
        return Json(new { backgroundUrl = s.BackgroundUrl ?? "/img/hallenboden.png" });
    }
}
