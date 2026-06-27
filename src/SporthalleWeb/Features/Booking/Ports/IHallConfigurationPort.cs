namespace SporthalleWeb.Features.Booking.Ports;

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
