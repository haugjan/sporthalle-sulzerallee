using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Calendar;

public sealed class GetWeekSlots(IBookingSlots slotRepo, IHallMembers members)
{
    public async Task<IReadOnlyList<WeekSlotDto>> ExecuteAsync(DateOnly monday)
    {
        var fromUtc = new DateTime(monday.Year, monday.Month, monday.Day, 0, 0, 0, DateTimeKind.Utc);
        var toUtc = fromUtc.AddDays(7);
        var slots = await slotRepo.GetForWeekAsync(fromUtc, toUtc);

        // The display colour comes from the renting member; cache lookups per member.
        var colorByMember = new Dictionary<int, string?>();
        var result = new List<WeekSlotDto>();
        foreach (var s in slots.Where(s => s.Type != SlotType.Rejected))
        {
            string? memberColor = null;
            if (s.MemberId is int memberId)
            {
                if (!colorByMember.TryGetValue(memberId, out memberColor))
                {
                    memberColor = (await members.FindByIdAsync(memberId))?.Color;
                    colorByMember[memberId] = memberColor;
                }
            }

            result.Add(new WeekSlotDto(
                Id: s.Id,
                StartUtc: s.Slot.StartUtc,
                EndUtc: s.Slot.EndUtc,
                Type: s.Type.ToString(),
                Color: SlotDisplayColor.For(s.Type, memberColor),
                Title: s.ShowTitlePublic ? s.Title : ""));
        }
        return result;
    }
}
