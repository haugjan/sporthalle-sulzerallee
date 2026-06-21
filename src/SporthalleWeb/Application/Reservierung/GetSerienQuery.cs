using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed record SerieWithYearCount(RecurringSlot Serie, int OccurrencesThisYear);

public sealed class GetSerienQuery(IRecurringSlotRepository serieRepo)
{
    public async Task<IReadOnlyList<SerieWithYearCount>> GetForYearAsync(int year)
    {
        var serien = await serieRepo.GetByYearAsync(year);
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd   = new DateOnly(year, 12, 31);
        return serien.Select(s =>
        {
            var count = s.GenerateOccurrences().Count(o => o.Date >= yearStart && o.Date <= yearEnd);
            return new SerieWithYearCount(s, count);
        }).ToList();
    }
}
