namespace SporthalleWeb.Features.Booking.Ports;

/// <summary>
/// Raw key-value access to the <c>HallConfig</c> table. The typed, domain-shaped
/// configuration reader is <see cref="IHallConfiguration"/>; this port is the
/// low-level store both that reader and the admin configuration editor build on.
/// </summary>
public interface IHallConfigStore
{
    Task<string?> GetAsync(string key);

    Task<Dictionary<string, string?>> GetAllAsync();

    Task SetManyAsync(Dictionary<string, string?> values);
}
