using SporthalleWeb.Domain.Booking.RecurringAggregate;

namespace SporthalleWeb.Features.Booking.Ports;

public interface IRecurringSlots
{
    Task<IReadOnlyList<RecurringSlot>> GetByYearAsync(int year);
    Task<RecurringSlot?> FindByIdAsync(int id);
    Task<int> SaveAsync(RecurringSlot slot);
    Task UpdateAsync(RecurringSlot slot);
    Task DeleteAsync(int id);
}
