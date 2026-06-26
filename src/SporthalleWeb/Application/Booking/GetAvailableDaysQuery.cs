using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;

namespace SporthalleWeb.Application.Booking;

public sealed class GetAvailableDaysQuery(
    IBookingSlotRepository slotRepo,
    IHallConfigurationPort config)
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<IReadOnlyList<string>> GetAsync(string month, int durationMinutes)
    {
        if (!int.TryParse(month.Split('-')[0], out var year) ||
            !int.TryParse(month.Split('-').ElementAtOrDefault(1) ?? "", out var monthNum))
            return [];

        var openStart = await config.GetOpeningHourStartAsync();
        var openEnd = await config.GetOpeningHourEndAsync();
        var blockMin = await config.GetBlockDurationMinutesAsync();
        var blocksNeeded = durationMinutes / blockMin;
        var totalBlocks = (openEnd - openStart) * (60 / blockMin);

        var firstDay = new DateOnly(year, monthNum, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var fromUtc = firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
        var toUtc = lastDay.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Local).ToUniversalTime();

        var existingSlots = await slotRepo.GetForWeekAsync(fromUtc, toUtc);

        var result = new List<string>();
        for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
        {
            if (d.ToDateTime(TimeOnly.MinValue) < DateTime.Today) continue;
            if (HasFreeBlock(d, existingSlots, openStart, blockMin, totalBlocks, blocksNeeded))
                result.Add(d.ToString("yyyy-MM-dd"));
        }
        return result;
    }

    private static bool HasFreeBlock(
        DateOnly date, IReadOnlyList<BookingSlot> slots,
        int openStart, int blockMin, int totalBlocks, int blocksNeeded)
    {
        var takenBlocks = new HashSet<int>();
        foreach (var s in slots)
        {
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(s.Slot.StartUtc, Zurich);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(s.Slot.EndUtc, Zurich);
            if (DateOnly.FromDateTime(localStart) != date) continue;
            var startBlock = ((localStart.Hour * 60 + localStart.Minute) -
                              openStart * 60) / blockMin;
            var endBlock = ((localEnd.Hour * 60 + localEnd.Minute) -
                            openStart * 60) / blockMin;
            for (var b = startBlock; b < endBlock; b++)
                takenBlocks.Add(b);
        }

        var consecutive = 0;
        for (var b = 0; b < totalBlocks; b++)
        {
            if (!takenBlocks.Contains(b))
            {
                consecutive++;
                if (consecutive >= blocksNeeded) return true;
            }
            else
            {
                consecutive = 0;
            }
        }
        return false;
    }
}
