namespace SporthalleWeb.Features.Booking.Ports;

public interface IHallConfigStore
{
    Task<string?> GetAsync(string key);

    Task<Dictionary<string, string?>> GetAllAsync();

    Task SetManyAsync(Dictionary<string, string?> values);
}
