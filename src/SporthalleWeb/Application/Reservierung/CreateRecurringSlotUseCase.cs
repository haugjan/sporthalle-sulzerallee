using System.Globalization;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed record SerieCommand(
    string Title,
    DayOfWeek Wochentag,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly SeriesStart,
    DateOnly SeriesEnd,
    string? Color,
    string? Notes);

public sealed record SerieConflictDate(DateOnly Date, string Label);
public sealed record SerieCheckResult(int OccurrenceCount, IReadOnlyList<SerieConflictDate> Conflicts);
public sealed record SerieCreateResult(int RecurringSlotId, int Created, int Skipped);

public sealed class CreateRecurringSlotUseCase(
    IRecurringSlotRepository serieRepo,
    IBookingSlotRepository slotRepo)
{
    private static readonly CultureInfo DeCh = CultureInfo.GetCultureInfo("de-CH");

    public async Task<SerieCheckResult> CheckConflictsAsync(SerieCommand cmd)
    {
        var temp = RecurringSlot.Create(cmd.Title, cmd.Wochentag, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Color, cmd.Notes, "");
        var occurrences = temp.GenerateOccurrences();
        var conflicts = new List<SerieConflictDate>();
        foreach (var (date, slot) in occurrences)
        {
            var overlaps = await slotRepo.GetActiveOverlapsAsync(slot);
            if (overlaps.Count > 0)
                conflicts.Add(new(date, date.ToString("dddd, d. MMMM yyyy", DeCh)));
        }
        return new SerieCheckResult(occurrences.Count, conflicts);
    }

    public async Task<SerieCreateResult> ExecuteAsync(SerieCommand cmd, string createdBy, bool skipConflicts)
    {
        var serie = RecurringSlot.Create(cmd.Title, cmd.Wochentag, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Color, cmd.Notes, createdBy);
        var serieId = await serieRepo.SaveAsync(serie);

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
            var booking = BookingSlot.CreateSerie(timeSlot, cmd.Title, cmd.Color, cmd.Notes, createdBy, serieId);
            await slotRepo.SaveAsync(booking);
            created++;
        }

        return new SerieCreateResult(serieId, created, skipped);
    }
}
