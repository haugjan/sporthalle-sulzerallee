using System.Globalization;
using SporthalleWeb.Domain.Booking.RecurringAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Recurring;

public sealed class UpdateRecurringSlot(
    IRecurringSlots serieRepo,
    IBookingSlots slotRepo)
{
    private static readonly CultureInfo DeCh = CultureInfo.GetCultureInfo("de-CH");

    public async Task<RecurringSlotCheckResult> CheckConflictsAsync(int serieId, RecurringSlotCommand cmd)
    {
        var temp = RecurringSlot.Create(cmd.Title, cmd.Weekday, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Notes, "");
        var occurrences = temp.GenerateOccurrences();
        var conflicts = new List<RecurringSlotConflictDate>();
        foreach (var (date, slot) in occurrences)
        {
            var overlaps = await slotRepo.GetActiveOverlapsExcludingSerieAsync(slot, serieId);
            if (overlaps.Count > 0)
                conflicts.Add(new(date, date.ToString("dddd, d. MMMM yyyy", DeCh)));
        }
        return new RecurringSlotCheckResult(occurrences.Count, conflicts);
    }

    public async Task<RecurringSlotCreateResult> ExecuteAsync(int serieId, RecurringSlotCommand cmd, string updatedBy, bool skipConflicts)
    {
        var serie = await serieRepo.FindByIdAsync(serieId)
            ?? throw new DomainException("Serientermin nicht gefunden.");

        serie.Update(cmd.Title, cmd.Weekday, cmd.StartTime, cmd.EndTime,
            cmd.SeriesStart, cmd.SeriesEnd, cmd.Notes, cmd.IsBlocker, cmd.MemberId, cmd.ShowTitlePublic);

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
            var slotType = cmd.IsBlocker ? SlotType.Blocker : SlotType.Recurring;
            var booking = BookingSlot.CreateSerie(timeSlot, cmd.Title, cmd.Notes, updatedBy, serieId, slotType, cmd.MemberId, serie.ShowTitlePublic);
            await slotRepo.SaveAsync(booking);
            created++;
        }

        return new RecurringSlotCreateResult(created, skipped);
    }
}
