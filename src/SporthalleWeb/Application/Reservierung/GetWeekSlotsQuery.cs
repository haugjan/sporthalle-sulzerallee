using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class GetWeekSlotsQuery(IBookingSlotRepository slotRepo)
{
    public async Task<IReadOnlyList<WeekSlotDto>> ExecuteAsync(DateOnly monday)
    {
        var fromUtc = new DateTime(monday.Year, monday.Month, monday.Day, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(7);
        var slots = await slotRepo.GetForWeekAsync(fromUtc, toUtc);

        return slots
            .Where(s => s.Type != SlotType.Blocker)
            .Select(s => new WeekSlotDto(
                Id: s.Id,
                StartUtc: s.Slot.StartUtc,
                EndUtc: s.Slot.EndUtc,
                Type: s.Type.ToString(),
                Color: s.Color,
                Title: s.Title))
            .ToList();
    }
}
