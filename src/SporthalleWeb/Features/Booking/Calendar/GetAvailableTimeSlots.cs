using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Calendar;

public sealed class GetAvailableTimeSlots(
    IBookingSlots slotRepo,
    IHallConfiguration config)
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<IReadOnlyList<SlotOption>> GetAsync(DateOnly parsedDate, int durationMinutes)
    {
        var openStart = await config.GetOpeningHourStartAsync();
        var openEnd = await config.GetOpeningHourEndAsync();
        var blockMin = await config.GetBlockDurationMinutesAsync();
        var blocksNeeded = durationMinutes / blockMin;
        var totalBlocks = (openEnd - openStart) * (60 / blockMin);

        var fromUtc = parsedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var toUtc = parsedDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();
        var existingSlots = await slotRepo.GetForWeekAsync(fromUtc, toUtc);

        var takenBlocks = new HashSet<int>();
        foreach (var s in existingSlots)
        {
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(s.Slot.StartUtc, Zurich);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(s.Slot.EndUtc, Zurich);
            // Any overlap into a public block (even a few minutes from a 5-minute admin
            // booking) makes the whole block unavailable: floor the start, ceil the end.
            var startBlock = (int)Math.Floor((localStart.Hour * 60 + localStart.Minute - openStart * 60) / (double)blockMin);
            var endBlock = (int)Math.Ceiling((localEnd.Hour * 60 + localEnd.Minute - openStart * 60) / (double)blockMin);
            for (var b = startBlock; b < endBlock; b++)
                takenBlocks.Add(b);
        }

        var result = new List<SlotOption>();
        for (var b = 0; b <= totalBlocks - blocksNeeded; b++)
        {
            var hasConflict = false;
            for (var i = 0; i < blocksNeeded; i++)
                if (takenBlocks.Contains(b + i)) { hasConflict = true; break; }

            var startMin = openStart * 60 + b * blockMin;
            var endMin = startMin + durationMinutes;
            var startLocal = parsedDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(startMin)));
            var endLocal = parsedDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMin)));
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, Zurich);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, Zurich);

            result.Add(new SlotOption(
                StartUtc: startUtc,
                EndUtc: endUtc,
                StartLocal: startLocal.ToString("HH:mm"),
                EndLocal: endLocal.ToString("HH:mm"),
                IsAvailable: !hasConflict));
        }
        return result;
    }
}
