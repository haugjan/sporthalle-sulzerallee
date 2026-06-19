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

    private static BookingSlot MapToDomain(BookingSlotRecord r) =>
        BookingSlot.FromPersistence(
            id: r.Id,
            renterId: r.RenterId,
            recurringRuleId: r.RecurringRuleId,
            status: r.Status,
            startUtc: DateTime.SpecifyKind(r.StartUtc, DateTimeKind.Utc),
            endUtc: DateTime.SpecifyKind(r.EndUtc, DateTimeKind.Utc),
            isRecurringSlot: r.IsRecurringSlot,
            color: r.Color,
            eventType: r.EventType,
            notes: r.Notes,
            createdAt: DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc),
            updatedAt: DateTime.SpecifyKind(r.UpdatedAt, DateTimeKind.Utc),
            createdBy: r.CreatedBy);
}
