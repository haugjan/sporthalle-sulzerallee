using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public sealed record FloorPlanViewModel(string SiteKey, string? BackgroundUrl, string? LineColor);

[Route("passivmitglieder/hallenboden")]
public sealed partial class FloorPlanController(IConfiguration config) : Controller
{
    [HttpGet("")]
    public IActionResult Index(string? bg = null, string? line = null)
    {
        var siteKey = config["Turnstile:SiteKey"] is { Length: > 0 } k ? k : "1x00000000000000000000AA";
        var model = new FloorPlanViewModel(siteKey, SanitizeUrl(bg), SanitizeColor(line));
        return View("~/Features/PassiveMembership/Registration/Views/FloorPlan.cshtml", model);
    }

    private static string? SanitizeUrl(string? value) =>
        value is { Length: > 0 } v && UrlPattern().IsMatch(v) ? v : null;

    private static string? SanitizeColor(string? value) =>
        value is { Length: > 0 } v && ColorPattern().IsMatch(v.Trim()) ? v.Trim() : null;

    [GeneratedRegex("^(/|https?://)[^\\s\"'<>]+$")]
    private static partial Regex UrlPattern();

    [GeneratedRegex("^(#[0-9a-fA-F]{3,8}|rgba?\\([0-9.,%\\s]+\\))$")]
    private static partial Regex ColorPattern();
}
