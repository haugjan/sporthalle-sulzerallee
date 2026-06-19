namespace SporthalleWeb.Application.Reservierung;

public sealed class CreateRecurringRuleCommand
{
    public int? MemberId { get; set; }
    public string Description { get; set; } = "";
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = "08:00";
    public string EndTime { get; set; } = "09:00";
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidUntil { get; set; }
    public int IntervalWeeks { get; set; } = 1;
    public bool ExcludeSchoolHolidays { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
}
