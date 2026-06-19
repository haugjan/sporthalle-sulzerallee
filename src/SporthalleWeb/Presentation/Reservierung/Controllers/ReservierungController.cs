using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Application.Reservierung;

namespace SporthalleWeb.Presentation.Reservierung.Controllers;

[ApiController]
[Route("api/reservierung")]
public sealed class ReservierungController(GetWeekSlotsQuery weekSlotsQuery) : ControllerBase
{
    [HttpGet("wochen-slots")]
    public async Task<IActionResult> GetWochenSlots([FromQuery] string von)
    {
        if (!DateOnly.TryParseExact(von, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var date))
            return BadRequest("Parameter 'von' muss im Format YYYY-MM-DD angegeben werden.");

        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        var monday = date.AddDays(-daysFromMonday);

        var slots = await weekSlotsQuery.ExecuteAsync(monday);
        return Ok(slots);
    }
}
