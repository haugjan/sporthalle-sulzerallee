namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record CreateRecurringRuleRequest(
    int? MemberId,
    string Description,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string ValidFrom,
    string ValidUntil,
    int IntervalWeeks,
    bool ExcludeSchoolHolidays,
    string? Color,
    string? Notes);
