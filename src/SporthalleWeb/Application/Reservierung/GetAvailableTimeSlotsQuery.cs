using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class GetAvailableTimeSlotsQuery(
    IBookingSlotRepository slotRepo,
    IHallConfigurationPort config)
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<IReadOnlyList<SlotOption>> GetAsync(string datum, int dauernMinuten)
    {
        if (!DateOnly.TryParseExact(datum, "yyyy-MM-dd", null,
                System.Globalization.DateTimeStyles.None, out var date))
            return [];

        var openStart = await config.GetOpeningHourStartAsync();
        var openEnd = await config.GetOpeningHourEndAsync();
        var blockMin = await config.GetBlockDurationMinutesAsync();
        var blocksNeeded = dauernMinuten / blockMin;
        var totalBlocks = (openEnd - openStart) * (60 / blockMin);

        var fromUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var toUtc = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();
        var existingSlots = await slotRepo.GetForWeekAsync(fromUtc, toUtc);

        var takenBlocks = new HashSet<int>();
        foreach (var s in existingSlots)
        {
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(s.Slot.StartUtc, Zurich);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(s.Slot.EndUtc, Zurich);
            var startBlock = (localStart.Hour * 60 + localStart.Minute - openStart * 60) / blockMin;
            var endBlock = (localEnd.Hour * 60 + localEnd.Minute - openStart * 60) / blockMin;
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
            var endMin = startMin + dauernMinuten;
            var startLocal = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(startMin)));
            var endLocal = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(endMin)));
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
