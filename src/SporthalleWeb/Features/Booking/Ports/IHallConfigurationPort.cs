using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public interface IHallConfiguration
{
    Task<int> GetBlockDurationMinutesAsync();
    Task<int> GetOpeningHourStartAsync();
    Task<int> GetOpeningHourEndAsync();
    Task<int> GetShortNoticeDaysAsync();
    Task<DateOnly?> GetBookingCutoffDateAsync();
    Task<IReadOnlyList<int>> GetBookableDurationsAsync();
    Task<IReadOnlyList<string>> GetEventTypesAsync();
    Task<string?> GetPriceTextAsync();
}
