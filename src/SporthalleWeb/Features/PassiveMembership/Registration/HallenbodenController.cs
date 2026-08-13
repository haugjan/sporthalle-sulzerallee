using Microsoft.AspNetCore.Mvc;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

[Route("hallenboden")]
public sealed class HallenbodenController(GetFieldStatuses fieldStatuses) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await fieldStatuses.ExecuteAsync();
        return View("~/Features/PassiveMembership/Registration/Views/Hallenboden.cshtml", result.OccupiedFields.Count);
    }
}
