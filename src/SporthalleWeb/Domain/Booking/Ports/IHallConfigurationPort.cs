namespace SporthalleWeb.Domain.Booking.Ports;

public interface IHallConfigurationPort
{
    Task<int> GetBlockDurationMinutesAsync();
    Task<int> GetOpeningHourStartAsync();
    Task<int> GetOpeningHourEndAsync();
    Task<int> GetShortNoticeDaysAsync();
    Task<int?> GetMaxBookingDaysAsync();
    Task<IReadOnlyList<int>> GetBookableDurationsAsync();
    Task<IReadOnlyList<string>> GetEventTypesAsync();
    Task<string?> GetPreisTextAsync();
}
