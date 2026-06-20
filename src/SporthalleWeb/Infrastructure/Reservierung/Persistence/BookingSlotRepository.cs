using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence;

public sealed class BookingSlotRepository(IScopeProvider scopeProvider) : IBookingSlotRepository
{
    public async Task<IReadOnlyList<BookingSlot>> GetForWeekAsync(DateTime fromUtc, DateTime toUtc)
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE StartUtc >= @0 AND StartUtc < @1",
            fromUtc, toUtc);
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsAsync(TimeSlot slot)
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE StartUtc < @0 AND EndUtc > @1 AND Type != @2",
            slot.EndUtc, slot.StartUtc, SlotType.Rejected.ToString());
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<BookingSlot?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<BookingSlotRecord>(
            new Sql("SELECT * FROM BookingSlots WHERE Id = @0", id));
        scope.Complete();
        return record is null ? null : MapToDomain(record);
    }

    public async Task<BookingSlot> CheckConflictAndSaveAsync(BookingSlot booking, TimeSlot slot)
    {
        using var scope = scopeProvider.CreateScope();

        var overlapSql = new Sql(
            "SELECT * FROM BookingSlots WHERE StartUtc < @0 AND EndUtc > @1 AND Type != @2",
            slot.EndUtc, slot.StartUtc, SlotType.Rejected.ToString());
        var overlaps = await scope.Database.FetchAsync<BookingSlotRecord>(overlapSql);
        if (overlaps.Count > 0)
            throw new SlotConflictException(slot, overlaps.Select(MapToDomain).ToList());

        var record = MapToRecord(booking);
        record.Id = Convert.ToInt32(await scope.Database.InsertAsync(record));
        scope.Complete();
        return MapToDomain(record);
    }

    public async Task<BookingSlot> SaveAsync(BookingSlot slot)
    {
        using var scope = scopeProvider.CreateScope();
        var record = MapToRecord(slot);
        record.Id = Convert.ToInt32(await scope.Database.InsertAsync(record));
        scope.Complete();
        return MapToDomain(record);
    }

    public async Task UpdateAsync(BookingSlot slot)
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
        await scope.Database.ExecuteAsync(new Sql("DELETE FROM BookingSlots WHERE Id = @0", id));
        scope.Complete();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetForMemberAsync(int memberId)
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE MemberId = @0 ORDER BY StartUtc DESC",
            memberId);
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetReservedSlotsAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE Type = @0 ORDER BY StartUtc",
            SlotType.Reserved.ToString());
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetAllAsync(DateOnly? from, DateOnly? to, SlotType? type, bool includeRejected = false)
    {
        using var scope = scopeProvider.CreateScope();
        var conditions = new List<string>();
        var args = new List<object>();
        int idx = 0;

        if (from is not null)
        {
            conditions.Add($"StartUtc >= @{idx++}");
            args.Add(from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }
        if (to is not null)
        {
            conditions.Add($"StartUtc < @{idx++}");
            args.Add(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        }
        if (type is not null)
        {
            conditions.Add($"Type = @{idx++}");
            args.Add(type.ToString()!);
        }
        else if (!includeRejected)
        {
            conditions.Add($"Type != @{idx++}");
            args.Add(SlotType.Rejected.ToString());
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var sql = new Sql($"SELECT * FROM BookingSlots {where} ORDER BY StartUtc DESC", args.ToArray());
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    private static BookingSlot MapToDomain(BookingSlotRecord r) =>
        BookingSlot.FromPersistence(
            id: r.Id,
            memberId: r.MemberId,
            type: r.Type,
            startUtc: DateTime.SpecifyKind(r.StartUtc, DateTimeKind.Utc),
            endUtc: DateTime.SpecifyKind(r.EndUtc, DateTimeKind.Utc),
            title: r.Title,
            color: r.Color,
            notes: r.Notes,
            createdAt: DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc),
            updatedAt: DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc),
            createdBy: r.CreatedBy);

    private static BookingSlotRecord MapToRecord(BookingSlot s) =>
        new()
        {
            MemberId = s.MemberId,
            Type = s.Type.ToString(),
            StartUtc = s.Slot.StartUtc,
            EndUtc = s.Slot.EndUtc,
            Title = s.Title,
            Color = s.Color,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            CreatedBy = s.CreatedBy
        };
}
