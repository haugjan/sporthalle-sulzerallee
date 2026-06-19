using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Reservierung.Persistence;

public sealed class RecurringRuleRepository(IScopeProvider scopeProvider) : IRecurringRuleRepository
{
    public async Task<RecurringRule> SaveAsync(RecurringRule rule)
    {
        using var scope = scopeProvider.CreateScope();
        var record = MapToRecord(rule);
        record.Id = (int)(await scope.Database.InsertAsync(record))!;
        scope.Complete();
        return RecurringRule.FromPersistence(
            record.Id, record.MemberId, record.Description, record.DayOfWeek,
            record.StartTime, record.EndTime, record.ValidFrom, record.ValidUntil,
            record.IntervalWeeks, record.IsActive, record.ExcludeSchoolHolidays,
            record.Color, record.Notes, record.CreatedAt, record.CreatedBy);
    }

    public async Task<IReadOnlyList<RecurringRule>> GetActiveRulesAsync()
    {
        using var scope = scopeProvider.CreateScope();
        var records = await scope.Database.FetchAsync<RecurringRuleRecord>(
            new Sql("SELECT * FROM RecurringRules WHERE IsActive = 1 ORDER BY DayOfWeek, StartTime"));
        scope.Complete();
        return records.Select(MapToDomain).ToList();
    }

    public async Task<RecurringRule?> FindByIdAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<RecurringRuleRecord>(
            new Sql("SELECT * FROM RecurringRules WHERE Id = @0", id));
        scope.Complete();
        return record is null ? null : MapToDomain(record);
    }

    public async Task DeactivateAsync(int id)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("UPDATE RecurringRules SET IsActive = 0 WHERE Id = @0", id));
        scope.Complete();
    }

    private static RecurringRule MapToDomain(RecurringRuleRecord r) =>
        RecurringRule.FromPersistence(
            r.Id, r.MemberId, r.Description, r.DayOfWeek,
            r.StartTime, r.EndTime,
            DateTime.SpecifyKind(r.ValidFrom, DateTimeKind.Utc),
            DateTime.SpecifyKind(r.ValidUntil, DateTimeKind.Utc),
            r.IntervalWeeks, r.IsActive, r.ExcludeSchoolHolidays,
            r.Color, r.Notes,
            DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc),
            r.CreatedBy);

    private static RecurringRuleRecord MapToRecord(RecurringRule r) =>
        new()
        {
            MemberId = r.MemberId,
            Description = r.Description,
            DayOfWeek = (int)r.DayOfWeek,
            StartTime = r.StartTime.ToString("HH:mm"),
            EndTime = r.EndTime.ToString("HH:mm"),
            ValidFrom = r.ValidFrom.ToDateTime(TimeOnly.MinValue),
            ValidUntil = r.ValidUntil.ToDateTime(TimeOnly.MinValue),
            IntervalWeeks = r.IntervalWeeks,
            IsActive = r.IsActive,
            ExcludeSchoolHolidays = r.ExcludeSchoolHolidays,
            Color = r.Color,
            Notes = r.Notes,
            CreatedAt = r.CreatedAt,
            CreatedBy = r.CreatedBy
        };
}
