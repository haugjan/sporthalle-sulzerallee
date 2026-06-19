namespace SporthalleWeb.Domain.Reservierung;

public sealed class RecurringRule
{
    public int Id { get; private set; }
    public int? MemberId { get; private set; }
    public string Description { get; private set; } = "";
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public int IntervalWeeks { get; private set; }
    public bool IsActive { get; private set; }
    public bool ExcludeSchoolHolidays { get; private set; }
    public string? Color { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "";

    private RecurringRule() { }

    public static RecurringRule Create(
        int? memberId, string description, DayOfWeek dayOfWeek,
        TimeOnly startTime, TimeOnly endTime, DateOnly validFrom, DateOnly validUntil,
        int intervalWeeks, bool excludeSchoolHolidays, string? color, string? notes,
        string createdBy) =>
        new()
        {
            MemberId = memberId,
            Description = description,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            IntervalWeeks = intervalWeeks <= 0 ? 1 : intervalWeeks,
            IsActive = true,
            ExcludeSchoolHolidays = excludeSchoolHolidays,
            Color = color,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public static RecurringRule FromPersistence(
        int id, int? memberId, string description, int dayOfWeek,
        string startTime, string endTime, DateTime validFrom, DateTime validUntil,
        int intervalWeeks, bool isActive, bool excludeSchoolHolidays,
        string? color, string? notes, DateTime createdAt, string createdBy) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            Description = description,
            DayOfWeek = (DayOfWeek)dayOfWeek,
            StartTime = TimeOnly.ParseExact(startTime, "HH:mm"),
            EndTime = TimeOnly.ParseExact(endTime, "HH:mm"),
            ValidFrom = DateOnly.FromDateTime(validFrom),
            ValidUntil = DateOnly.FromDateTime(validUntil),
            IntervalWeeks = intervalWeeks,
            IsActive = isActive,
            ExcludeSchoolHolidays = excludeSchoolHolidays,
            Color = color,
            Notes = notes,
            CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
            CreatedBy = createdBy
        };

    public void Deactivate() => IsActive = false;

    public IReadOnlyList<DateOnly> GenerateDates(
        DateOnly from, DateOnly until,
        IReadOnlyList<(DateOnly From, DateOnly Until)> holidays)
    {
        var dates = new List<DateOnly>();
        var start = ValidFrom > from ? ValidFrom : from;
        var end = ValidUntil < until ? ValidUntil : until;

        var current = start;
        while (current <= end && current.DayOfWeek != DayOfWeek)
            current = current.AddDays(1);

        int occurrenceIndex = 0;
        while (current <= end)
        {
            if (occurrenceIndex % IntervalWeeks == 0)
            {
                bool inHoliday = ExcludeSchoolHolidays &&
                    holidays.Any(h => current >= h.From && current <= h.Until);
                if (!inHoliday)
                    dates.Add(current);
            }
            current = current.AddDays(7);
            occurrenceIndex++;
        }
        return dates;
    }
}
