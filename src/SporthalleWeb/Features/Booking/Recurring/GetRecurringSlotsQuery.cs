using SporthalleWeb.Domain.Booking.RecurringAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Recurring;

public sealed record RecurringSlotWithYearCount(RecurringSlot Slot, int OccurrencesThisYear);

public sealed class GetRecurringSlots(IRecurringSlots serieRepo)
{
    public async Task<IReadOnlyList<RecurringSlotWithYearCount>> GetForYearAsync(int year)
    {
        var serien = await serieRepo.GetByYearAsync(year);
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd   = new DateOnly(year, 12, 31);
        return serien.Select(s =>
        {
            var count = s.GenerateOccurrences().Count(o => o.Date >= yearStart && o.Date <= yearEnd);
            return new RecurringSlotWithYearCount(s, count);
        }).ToList();
    }
}
