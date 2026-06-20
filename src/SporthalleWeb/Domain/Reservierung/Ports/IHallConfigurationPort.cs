namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IHallConfigurationPort
{
    Task<int> GetBlockDurationMinutesAsync();
    Task<int> GetOpeningHourStartAsync();
    Task<int> GetOpeningHourEndAsync();
    Task<DateOnly?> GetBuchungenBisDatumAsync();
    Task<IReadOnlyList<int>> GetBuchbareDauernAsync();
    Task<IReadOnlyList<string>> GetAnlasseAsync();
    Task<string?> GetPreisTextAsync();
}
