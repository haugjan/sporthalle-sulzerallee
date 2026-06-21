using System.Globalization;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class UpdateRecurringSlotUseCase(
    IRecurringSlotRepository serieRepo,
    IBookingSlotRepository slotRepo)
{
    private static readonly CultureInfo DeCh = CultureInfo.GetCultureInfo("de-CH");

    public async Task<SerieCheckResult> CheckConflictsAsync(int serieId, SerieCommand cmd)
    {
        var temp = RecurringSlot.Create(cmd.Title, cmd.Wochentag, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Color, cmd.Notes, "");
        var occurrences = temp.GenerateOccurrences();
        var conflicts = new List<SerieConflictDate>();
        foreach (var (date, slot) in occurrences)
        {
            var overlaps = await slotRepo.GetActiveOverlapsExcludingSerieAsync(slot, serieId);
            if (overlaps.Count > 0)
                conflicts.Add(new(date, date.ToString("dddd, d. MMMM yyyy", DeCh)));
        }
        return new SerieCheckResult(occurrences.Count, conflicts);
    }

    public async Task<SerieCreateResult> ExecuteAsync(int serieId, SerieCommand cmd, string updatedBy, bool skipConflicts)
    {
        var serie = await serieRepo.FindByIdAsync(serieId)
            ?? throw new DomainException("Serientermin nicht gefunden.");

        serie.Update(cmd.Title, cmd.Wochentag, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Color, cmd.Notes);

        await serieRepo.UpdateAsync(serie);
        await slotRepo.DeleteByRecurringSlotIdAsync(serieId);

        var occurrences = serie.GenerateOccurrences();
        var created = 0;
        var skipped = 0;

        foreach (var (_, timeSlot) in occurrences)
        {
            if (skipConflicts)
            {
                var overlaps = await slotRepo.GetActiveOverlapsAsync(timeSlot);
                if (overlaps.Count > 0) { skipped++; continue; }
            }
            var booking = BookingSlot.CreateSerie(timeSlot, cmd.Title, cmd.Color, cmd.Notes, updatedBy, serieId);
            await slotRepo.SaveAsync(booking);
            created++;
        }

        return new SerieCreateResult(serieId, created, skipped);
    }
}
