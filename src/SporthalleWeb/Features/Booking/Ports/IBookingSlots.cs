using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Features.Booking.Ports;

public interface IBookingSlots
{
    Task<IReadOnlyList<BookingSlot>> GetForWeekAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsAsync(TimeSlot slot);
    Task<BookingSlot> CheckConflictAndSaveAsync(BookingSlot booking, TimeSlot slot);
    Task<BookingSlot?> FindByIdAsync(int id);
    Task<BookingSlot> SaveAsync(BookingSlot slot);
    Task UpdateAsync(BookingSlot slot);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<BookingSlot>> GetForMemberAsync(int memberId);
    Task<IReadOnlyList<BookingSlot>> GetReservedSlotsAsync();
    Task<IReadOnlyList<BookingSlot>> GetAllAsync(DateOnly? from, DateOnly? to, SlotType? type, bool includeRejected = false);
    Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsExcludingSerieAsync(TimeSlot slot, int excludeRecurringSlotId);
    Task DeleteByRecurringSlotIdAsync(int recurringSlotId);
}
