namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IHallConfigurationPort
{
    Task<decimal> GetPricePerBlockAsync();
    Task<int> GetBlockDurationMinutesAsync();
    Task<int> GetOpeningHourStartAsync();
    Task<int> GetOpeningHourEndAsync();
    Task<int> GetMaxWeeksAheadAsync();
    Task<IReadOnlyList<int>> GetBuchbareDauernAsync();
    Task<IReadOnlyList<string>> GetAnlasseAsync();
}
