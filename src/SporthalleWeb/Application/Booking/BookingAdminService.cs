using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;

namespace SporthalleWeb.Application.Booking;

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

    public async Task<IReadOnlyList<BookingSlot>> GetSlotsForPeriodAsync(DateOnly from, DateOnly toInclusive, bool includeRejected = false)
        => await slotRepo.GetAllAsync(from, toInclusive, null, includeRejected);

    public async Task<IReadOnlyList<BookingSlot>> GetBlockersForPeriodAsync(DateOnly from, DateOnly toInclusive)
        => await slotRepo.GetAllAsync(from, toInclusive, SlotType.Blocker);

    public async Task<IReadOnlyList<(BookingSlot Slot, HallMember? Member)>> GetSlotsWithMembersForPeriodAsync(DateOnly from, DateOnly toInclusive, bool includeRejected = false)
    {
        var allSlots = await slotRepo.GetAllAsync(from, toInclusive, null, includeRejected);
        var slots = allSlots.Where(s => s.Type != SlotType.Blocker).ToList();
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

    public async Task ReactivateSlotAsync(int slotId, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var overlaps = await slotRepo.GetActiveOverlapsAsync(slot.Slot);
        if (overlaps.Count > 0)
            throw new DomainException("Dieser Zeitslot überschneidet sich mit einer bestehenden Buchung und kann nicht reaktiviert werden.");
        slot.Reactivate();
        await slotRepo.UpdateAsync(slot);
        await audit.LogAsync("BookingSlot", slotId, "Reactivated", adminUser, null, null);
    }

    public async Task UpdateSlotAsync(int slotId, string title, string? color, string? notes, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        slot.Update(title, color, notes);
        await slotRepo.UpdateAsync(slot);
        await audit.LogAsync("BookingSlot", slotId, "Updated", adminUser, null, new { title, color, notes });
    }

    public async Task<(BookingSlot? Slot, HallMember? Member)> FindSlotWithMemberAsync(int slotId)
    {
        var slot = await slotRepo.FindByIdAsync(slotId);
        if (slot is null) return (null, null);
        HallMember? member = slot.MemberId.HasValue
            ? await members.FindByIdAsync(slot.MemberId.Value)
            : null;
        return (slot, member);
    }

    public async Task RescheduleSlotAsync(int slotId, DateTime startUtc, DateTime endUtc, string adminUser)
    {
        var slot = await slotRepo.FindByIdAsync(slotId)
            ?? throw new DomainException($"Buchung {slotId} nicht gefunden.");
        var newSlot = new TimeSlot(startUtc, endUtc);
        var overlaps = await slotRepo.GetActiveOverlapsAsync(newSlot);
        if (overlaps.Any(o => o.Id != slotId))
            throw new DomainException("Der neue Zeitraum überschneidet sich mit einer bestehenden Buchung.");
        slot.Reschedule(newSlot);
        await slotRepo.UpdateAsync(slot);
        await audit.LogAsync("BookingSlot", slotId, "Rescheduled", adminUser, null, new { startUtc, endUtc });
    }

    public async Task UpdateMemberAsync(int memberId, string contactPerson, string? phone,
        string renterType, string billingName, string billingAddress,
        string billingPostalCode, string billingCity, string adminUser)
    {
        var member = await members.FindByIdAsync(memberId)
            ?? throw new DomainException($"Mieter {memberId} nicht gefunden.");
        var spaceIdx = contactPerson.IndexOf(' ');
        var firstName = spaceIdx >= 0 ? contactPerson[..spaceIdx].Trim() : contactPerson;
        var lastName  = spaceIdx >= 0 ? contactPerson[(spaceIdx + 1)..].Trim() : "";
        await members.UpdateProfileAsync(memberId, billingName,
            firstName, lastName,
            billingAddress, null,
            billingPostalCode, billingCity, phone);
        await audit.LogAsync("HallMember", memberId, "Updated", adminUser, null,
            new { contactPerson, phone, renterType, billingName, billingAddress, billingPostalCode, billingCity });
    }
}
