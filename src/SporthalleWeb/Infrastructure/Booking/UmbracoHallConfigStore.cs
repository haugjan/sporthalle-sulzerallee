using NPoco;
using SporthalleWeb.Features.Booking.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SporthalleWeb.Infrastructure.Booking;

/// <summary>
/// NPoco-backed adapter for <see cref="IHallConfigStore"/>. Owns all raw SQL
/// against the <c>HallConfig</c> table; the application layer never touches the
/// database directly.
/// </summary>
public sealed class UmbracoHallConfigStore(IScopeProvider scopeProvider) : IHallConfigStore
{
    public async Task<string?> GetAsync(string key)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<HallConfigRecord>(
            new Sql("SELECT * FROM HallConfig WHERE [Key] = @0", key));
        scope.Complete();
        return record?.Value;
    }

    public async Task<Dictionary<string, string?>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<HallConfigRecord>(
            new Sql("SELECT * FROM HallConfig"));
        scope.Complete();
        return records.ToDictionary(r => r.Key, r => r.Value);
    }

    private async Task SetAsync(string key, string? value)
    {
        using var scope = scopeProvider.CreateScope();
        var existing = await scope.Database.FirstOrDefaultAsync<HallConfigRecord>(
            new Sql("SELECT * FROM HallConfig WHERE [Key] = @0", key));
        if (existing is null)
        {
            await scope.Database.InsertAsync(new HallConfigRecord { Key = key, Value = value });
        }
        else
        {
            existing.Value = value;
            await scope.Database.UpdateAsync(existing);
        }
        scope.Complete();
    }

    public async Task SetManyAsync(Dictionary<string, string?> values)
    {
        foreach (var kv in values)
            await SetAsync(kv.Key, kv.Value);
    }
}
