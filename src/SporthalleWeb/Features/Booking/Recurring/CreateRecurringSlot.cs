using System.Globalization;
using SporthalleWeb.Domain.Booking.RecurringAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Recurring;

public sealed record RecurringSlotCommand(
    string Title,
    DayOfWeek Weekday,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly SeriesStart,
    DateOnly SeriesEnd,
    string? Color,
    string? Notes,
    bool IsBlocker = false,
    int? MemberId = null,
    bool ShowTitlePublic = false);

public sealed record RecurringSlotConflictDate(DateOnly Date, string Label);
public sealed record RecurringSlotCheckResult(int OccurrenceCount, IReadOnlyList<RecurringSlotConflictDate> Conflicts);
public sealed record RecurringSlotCreateResult(int Created, int Skipped);

public sealed class CreateRecurringSlot(
    IRecurringSlots serieRepo,
    IBookingSlots slotRepo)
{
    private static readonly CultureInfo DeCh = CultureInfo.GetCultureInfo("de-CH");

    public async Task<RecurringSlotCheckResult> CheckConflictsAsync(RecurringSlotCommand cmd)
    {
        var temp = RecurringSlot.Create(cmd.Title, cmd.Weekday, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Color, cmd.Notes, "");
        var occurrences = temp.GenerateOccurrences();
        var conflicts = new List<RecurringSlotConflictDate>();
        foreach (var (date, slot) in occurrences)
        {
            var overlaps = await slotRepo.GetActiveOverlapsAsync(slot);
            if (overlaps.Count > 0)
                conflicts.Add(new(date, date.ToString("dddd, d. MMMM yyyy", DeCh)));
        }
        return new RecurringSlotCheckResult(occurrences.Count, conflicts);
    }

    public async Task<RecurringSlotCreateResult> ExecuteAsync(RecurringSlotCommand cmd, string createdBy, bool skipConflicts)
    {
        var serie = RecurringSlot.Create(cmd.Title, cmd.Weekday, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Color, cmd.Notes, createdBy, cmd.IsBlocker, cmd.MemberId, cmd.ShowTitlePublic);
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
            var slotType = cmd.IsBlocker ? SlotType.Blocker : SlotType.Recurring;
            var booking = BookingSlot.CreateSerie(timeSlot, cmd.Title, cmd.Color, cmd.Notes, createdBy, serieId, slotType, cmd.MemberId, cmd.ShowTitlePublic);
            await slotRepo.SaveAsync(booking);
            created++;
        }

        return new RecurringSlotCreateResult(created, skipped);
    }
}
