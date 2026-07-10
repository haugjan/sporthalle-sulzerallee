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

    [HttpGet("debug")]
    public async Task<IActionResult> Debug()
    {
        var s = await settings.GetAsync();
        var raw = await settings.GetRawRasterAsync();
        return Json(new
        {
            backgroundUrl = s.BackgroundUrl,
            lineColor = s.LineColor,
            region = new { s.Region.X0, s.Region.Y0, s.Region.X1, s.Region.Y1 },
            specialAreas = s.SpecialFields.Areas.Count,
            rasterRaw = raw is { Length: > 400 } ? raw[..400] : raw
        });
    }
}
