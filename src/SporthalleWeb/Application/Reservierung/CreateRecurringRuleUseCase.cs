using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;
using SporthalleWeb.Infrastructure.Reservierung.Persistence;

namespace SporthalleWeb.Application.Reservierung;

public sealed class CreateRecurringRuleUseCase(
    IRecurringRuleRepository ruleRepo,
    IBookingSlotRepository slotRepo,
    IBookingAuditRepository audit,
    SchoolHolidayRepository holidayRepo)
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<RecurringRule> ExecuteAsync(CreateRecurringRuleCommand cmd, string adminUser)
    {
        var rule = RecurringRule.Create(
            memberId: cmd.MemberId,
            description: cmd.Description,
            dayOfWeek: (DayOfWeek)cmd.DayOfWeek,
            startTime: TimeOnly.ParseExact(cmd.StartTime, "HH:mm"),
            endTime: TimeOnly.ParseExact(cmd.EndTime, "HH:mm"),
            validFrom: cmd.ValidFrom,
            validUntil: cmd.ValidUntil,
            intervalWeeks: cmd.IntervalWeeks,
            excludeSchoolHolidays: cmd.ExcludeSchoolHolidays,
            color: cmd.Color,
            notes: cmd.Notes,
            createdBy: adminUser);

        rule = await ruleRepo.SaveAsync(rule);

        var holidays = await holidayRepo.GetRangesAsync();
        var slots = GenerateSlots(rule, adminUser, holidays);

        await slotRepo.SaveBatchAsync(slots);

        await audit.LogAsync("RecurringRule", rule.Id, "Created", adminUser, null,
            new { rule.Description, SlotsGenerated = slots.Count });

        return rule;
    }

    private static List<BookingSlot> GenerateSlots(
        RecurringRule rule, string adminUser,
        IReadOnlyList<(DateOnly From, DateOnly Until)> holidays)
    {
        var result = new List<BookingSlot>();
        var dates = rule.GenerateDates(rule.ValidFrom, rule.ValidUntil, holidays);

        foreach (var date in dates)
        {
            var startLocal = date.ToDateTime(rule.StartTime);
            var endLocal = date.ToDateTime(rule.EndTime);
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, Zurich);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, Zurich);
            result.Add(BookingSlot.CreateRecurringSlot(
                rule.MemberId, rule.Id, new TimeSlot(startUtc, endUtc), rule.Color, adminUser));
        }
        return result;
    }
}
