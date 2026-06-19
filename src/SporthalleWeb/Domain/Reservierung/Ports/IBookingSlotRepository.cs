namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IBookingSlotRepository
{
    Task<IReadOnlyList<BookingSlot>> GetForWeekAsync(DateTime fromUtc, DateTime toUtc);
    Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsAsync(TimeSlot slot);
    Task<BookingSlot?> FindByIdAsync(int id);
    Task<BookingSlot> SaveAsync(BookingSlot slot);
    Task UpdateAsync(BookingSlot slot);
    Task<IReadOnlyList<BookingSlot>> GetForMemberAsync(int memberId);
    Task<IReadOnlyList<BookingSlot>> GetForExportAsync(DateTime fromUtc, DateTime toUtc, bool confirmedOnly);
    Task<IReadOnlyList<BookingSlot>> GetPendingAdminApprovalAsync();
    Task SaveBatchAsync(IReadOnlyList<BookingSlot> slots);
}
