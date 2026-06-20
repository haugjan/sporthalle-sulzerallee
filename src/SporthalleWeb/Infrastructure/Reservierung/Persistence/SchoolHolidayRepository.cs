using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence;

public sealed record SchoolHoliday(int Id, string Name, DateOnly HolidayFrom, DateOnly HolidayUntil);

public sealed class SchoolHolidayRepository(IScopeProvider scopeProvider)
{
    public async Task<IReadOnlyList<SchoolHoliday>> GetAllAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<SchoolHolidayRecord>(
            new Sql("SELECT * FROM SchoolHolidays ORDER BY HolidayFrom"));
        scope.Complete();
        return records.Select(r => new SchoolHoliday(
            r.Id, r.Name,
            DateOnly.FromDateTime(r.HolidayFrom),
            DateOnly.FromDateTime(r.HolidayUntil))).ToList();
    }

    public async Task<SchoolHoliday> SaveAsync(string name, DateOnly from, DateOnly until)
    {
        using var scope = scopeProvider.CreateScope();
        var record = new SchoolHolidayRecord
        {
            Name = name,
            HolidayFrom = from.ToDateTime(TimeOnly.MinValue),
            HolidayUntil = until.ToDateTime(TimeOnly.MinValue),
            CreatedAt = DateTime.UtcNow
        };
        record.Id = (int)(await scope.Database.InsertAsync(record))!;
        scope.Complete();
        return new SchoolHoliday(record.Id, name, from, until);
    }

    public async Task DeleteAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("DELETE FROM SchoolHolidays WHERE Id = @0", id));
        scope.Complete();
    }

    public async Task<IReadOnlyList<(DateOnly From, DateOnly Until)>> GetRangesAsync()
    {
        var all = await GetAllAsync();
        return all.Select(h => (h.HolidayFrom, h.HolidayUntil)).ToList();
    }
}
