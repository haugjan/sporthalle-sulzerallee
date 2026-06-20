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
            "SELECT * FROM BookingSlots WHERE Status <> @0 AND StartUtc >= @1 AND StartUtc < @2",
            BookingStatus.Cancelled.ToString(), fromUtc, toUtc);
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetActiveOverlapsAsync(TimeSlot slot)
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE Status <> @0 AND StartUtc < @1 AND EndUtc > @2",
            BookingStatus.Cancelled.ToString(), slot.EndUtc, slot.StartUtc);
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

    public async Task<BookingSlot> SaveAsync(BookingSlot slot)
    {
        using var scope = scopeProvider.CreateScope();
        var record = MapToRecord(slot);
        record.Id = (int)(await scope.Database.InsertAsync(record))!;
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

    public async Task<IReadOnlyList<BookingSlot>> GetForMemberAsync(int memberId)
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE MemberId = @0 AND Status <> @1 ORDER BY StartUtc DESC",
            memberId, BookingStatus.Cancelled.ToString());
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetForExportAsync(
        DateTime fromUtc, DateTime toUtc, bool confirmedOnly)
    {
        using var scope = scopeProvider.CreateScope();
        var status = confirmedOnly ? BookingStatus.Confirmed.ToString() : "";
        Sql sql;
        if (confirmedOnly)
            sql = new Sql(
                "SELECT * FROM BookingSlots WHERE Status = @0 AND StartUtc >= @1 AND StartUtc < @2 ORDER BY StartUtc",
                status, fromUtc, toUtc);
        else
            sql = new Sql(
                "SELECT * FROM BookingSlots WHERE Status <> @0 AND StartUtc >= @1 AND StartUtc < @2 ORDER BY StartUtc",
                BookingStatus.Cancelled.ToString(), fromUtc, toUtc);
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetPendingAdminApprovalAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var sql = new Sql(
            "SELECT * FROM BookingSlots WHERE Status = @0 AND IsRecurringSlot = 0 ORDER BY StartUtc",
            BookingStatus.Provisional.ToString());
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<BookingSlot>> GetAllAsync(DateOnly? from, DateOnly? to, BookingStatus? status)
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
        if (status is not null)
        {
            conditions.Add($"Status = @{idx}");
            args.Add(status.ToString());
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var sql = new Sql($"SELECT * FROM BookingSlots {where} ORDER BY StartUtc DESC", args.ToArray());
        var records = await scope.Database.FetchAsync<BookingSlotRecord>(sql);
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task SaveBatchAsync(IReadOnlyList<BookingSlot> slots)
    {
        using var scope = scopeProvider.CreateScope();
        foreach (var slot in slots)
            await scope.Database.InsertAsync(MapToRecord(slot));
        scope.Complete();
    }

    private static BookingSlot MapToDomain(BookingSlotRecord r) =>
        BookingSlot.FromPersistence(
            id: r.Id,
            memberId: r.MemberId,
            recurringRuleId: r.RecurringRuleId,
            status: r.Status,
            startUtc: DateTime.SpecifyKind(r.StartUtc, DateTimeKind.Utc),
            endUtc: DateTime.SpecifyKind(r.EndUtc, DateTimeKind.Utc),
            pricePerBlock: r.PricePerBlock,
            totalBlocks: r.TotalBlocks,
            totalPrice: r.TotalPrice,
            priceNote: r.PriceNote,
            isRecurringSlot: r.IsRecurringSlot,
            color: r.Color,
            eventType: r.EventType,
            notes: r.Notes,
            createdAt: DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc),
            updatedAt: DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc),
            createdBy: r.CreatedBy);

    private static BookingSlotRecord MapToRecord(BookingSlot s) =>
        new()
        {
            MemberId = s.MemberId,
            RecurringRuleId = s.RecurringRuleId,
            Status = s.Status.ToString(),
            StartUtc = s.Slot.StartUtc,
            EndUtc = s.Slot.EndUtc,
            PricePerBlock = s.PricePerBlock,
            TotalBlocks = s.TotalBlocks,
            TotalPrice = s.TotalPrice,
            PriceNote = s.PriceNote,
            IsRecurringSlot = s.IsRecurringSlot,
            Color = s.Color,
            EventType = s.EventType,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            CreatedBy = s.CreatedBy
        };
}
