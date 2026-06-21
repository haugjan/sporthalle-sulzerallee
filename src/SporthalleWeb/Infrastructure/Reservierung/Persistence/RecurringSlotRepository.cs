using System.Globalization;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence;

public sealed class RecurringSlotRepository(IScopeProvider scopeProvider) : IRecurringSlotRepository
{
    public async Task<IReadOnlyList<RecurringSlot>> GetByYearAsync(int year)
    {
        using var scope = scopeProvider.CreateScope();
        var yearStart = new DateTime(year, 1, 1).ToString("yyyy-MM-dd");
        var yearEnd   = new DateTime(year, 12, 31).ToString("yyyy-MM-dd");
        var sql = new Sql(
            "SELECT * FROM RecurringSlots WHERE SeriesEnd >= @0 AND SeriesStart <= @1 ORDER BY SeriesStart, Wochentag, StartTime",
            yearStart, yearEnd);
        var records = await scope.Database.FetchAsync<RecurringSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<RecurringSlot?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<RecurringSlotRecord>(
            new Sql("SELECT * FROM RecurringSlots WHERE Id = @0", id));
        scope.Complete();
        return record is null ? null : MapToDomain(record);
    }

    public async Task<int> SaveAsync(RecurringSlot slot)
    {
        using var scope = scopeProvider.CreateScope();
        var record = MapToRecord(slot);
        await scope.Database.InsertAsync(record);
        scope.Complete();
        return (int)record.Id;
    }

    public async Task UpdateAsync(RecurringSlot slot)
    {
        using var scope = scopeProvider.CreateScope();
        var record = MapToRecord(slot);
        record.Id = slot.Id;
        await scope.Database.UpdateAsync(record);
        scope.Complete();
    }

    public async Task DeleteAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(new Sql("DELETE FROM RecurringSlots WHERE Id = @0", id));
        scope.Complete();
    }

    private static RecurringSlot MapToDomain(RecurringSlotRecord r) =>
        RecurringSlot.FromPersistence(
            id: (int)r.Id,
            title: r.Title,
            wochentag: r.Wochentag,
            startTime: r.StartTime,
            endTime: r.EndTime,
            seriesStart: r.SeriesStart,
            seriesEnd: r.SeriesEnd,
            color: r.Color,
            notes: r.Notes,
            createdBy: r.CreatedBy,
            createdAt: DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc),
            updatedAt: DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc));

    private static RecurringSlotRecord MapToRecord(RecurringSlot s) =>
        new()
        {
            Title = s.Title,
            Wochentag = (int)s.Wochentag,
            StartTime = s.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndTime = s.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            SeriesStart = s.SeriesStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SeriesEnd = s.SeriesEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Color = s.Color,
            Notes = s.Notes,
            CreatedBy = s.CreatedBy,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
}
