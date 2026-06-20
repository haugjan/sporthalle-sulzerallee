using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class BookingAdminService(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members,
    IBookingAuditRepository audit)
{
    public async Task<IReadOnlyList<(BookingSlot Slot, HallMember? Member)>> GetPendingAsync()
    {
        var slots = await slotRepo.GetReservedSlotsAsync();
        var result = new List<(BookingSlot, HallMember?)>();
        foreach (var slot in slots)
        {
            HallMember? member = null;
            if (slot.MemberId.HasValue)
                member = await members.FindByIdAsync(slot.MemberId.Value);
            result.Add((slot, member));
        }
        return result;
    }

    public async Task<BookingSlot> CreateSlotAsync(
        SlotType type, DateTime startUtc, DateTime endUtc,
        string title, string? color, string? notes,
        int? memberId, string adminUser)
    {
        var timeSlot = new TimeSlot(startUtc, endUtc);

        BookingSlot slot = type switch
        {
            SlotType.Blocker  => BookingSlot.CreateBlocker(timeSlot, title, color, notes, adminUser),
            SlotType.Reserved => BookingSlot.CreateReserved(memberId!.Value, timeSlot, title, color, notes, adminUser),
            SlotType.Booked   => BookingSlot.CreateBooked(memberId!.Value, timeSlot, title, color, notes, adminUser),
            _                 => throw new DomainException($"Unbekannter Slot-Typ: {type}")
        };

        var saved = await slotRepo.SaveAsync(slot);
        await audit.LogAsync("BookingSlot", saved.Id, "Created", adminUser,
            null, new { Type = saved.Type.ToString(), Title = saved.Title });
        return saved;
    }

    public async Task<IReadOnlyList<BookingSlot>> GetSlotsForPeriodAsync(DateOnly from, DateOnly toInclusive)
        => await slotRepo.GetAllAsync(from, toInclusive, null);

    public async Task<IReadOnlyList<(BookingSlot Slot, HallMember? Member)>> GetWeekSlotsWithMembersAsync(DateOnly monday)
    {
        var sunday = monday.AddDays(6);
        var slots = await slotRepo.GetAllAsync(monday, sunday, null);
        var result = new List<(BookingSlot, HallMember?)>();
        foreach (var slot in slots)
        {
            HallMember? member = slot.MemberId.HasValue
                ? await members.FindByIdAsync(slot.MemberId.Value)
                : null;
            result.Add((slot, member));
        }
        return result;
    }

    public async Task<IReadOnlyList<(BookingSlot Slot, HallMember? Member)>> GetSlotsWithMembersForDateAsync(DateOnly date)
    {
        var slots = await slotRepo.GetAllAsync(date, date, null);
        var result = new List<(BookingSlot, HallMember?)>();
        foreach (var slot in slots)
        {
            HallMember? member = slot.MemberId.HasValue
                ? await members.FindByIdAsync(slot.MemberId.Value)
                : null;
            result.Add((slot, member));
        }
        return result;
    }

    public async Task DeleteSlotAsync(int slotId, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        await slotRepo.DeleteAsync(slotId);
        await audit.LogAsync("BookingSlot", slotId, "Deleted", adminUser,
            new { Type = slot.Type.ToString() }, null);
    }
}
