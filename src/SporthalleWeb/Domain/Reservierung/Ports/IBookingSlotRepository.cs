namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IBookingSlotRepository
{
    Task<IReadOnlyList<BookingSlot>> GetForWeekAsync(DateTime fromUtc, DateTime toUtc);
}
