using System.Globalization;

namespace SporthalleWeb.Domain.Booking;

public sealed class RecurringSlot
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public int Id { get; private set; }
    public string Title { get; private set; } = "";
    public DayOfWeek Wochentag { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public DateOnly SeriesStart { get; private set; }
    public DateOnly SeriesEnd { get; private set; }
    public string? Color { get; private set; }
    public string? Notes { get; private set; }
    public bool IsBlocker { get; private set; }
    public int? MemberId { get; private set; }
    public bool ShowTitlePublic { get; private set; }
    public string CreatedBy { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private RecurringSlot() { }

    public static RecurringSlot Create(
        string title, DayOfWeek wochentag, TimeOnly startTime, TimeOnly endTime,
        DateOnly seriesStart, DateOnly seriesEnd, string? color, string? notes, string createdBy,
        bool isBlocker = false, int? memberId = null, bool showTitlePublic = false) =>
        new()
        {
            Title = title,
            Wochentag = wochentag,
            StartTime = startTime,
            EndTime = endTime,
            SeriesStart = seriesStart,
            SeriesEnd = seriesEnd,
            Color = color,
            Notes = notes,
            CreatedBy = createdBy,
            IsBlocker = isBlocker,
            MemberId = memberId,
            ShowTitlePublic = showTitlePublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static RecurringSlot FromPersistence(
        int id, string title, int wochentag, string startTime, string endTime,
        string seriesStart, string seriesEnd, string? color, string? notes,
        string createdBy, DateTime createdAt, DateTime updatedAt,
        bool isBlocker = false, int? memberId = null, bool showTitlePublic = false) =>
        new()
        {
            Id = id,
            Title = title,
            Wochentag = (DayOfWeek)wochentag,
            StartTime = TimeOnly.Parse(startTime, CultureInfo.InvariantCulture),
            EndTime = TimeOnly.Parse(endTime, CultureInfo.InvariantCulture),
            SeriesStart = DateOnly.Parse(seriesStart, CultureInfo.InvariantCulture),
            SeriesEnd = DateOnly.Parse(seriesEnd, CultureInfo.InvariantCulture),
            Color = color,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc),
            IsBlocker = isBlocker,
            MemberId = memberId,
            ShowTitlePublic = showTitlePublic
        };

    public void Update(
        string title, DayOfWeek wochentag, TimeOnly startTime, TimeOnly endTime,
        DateOnly seriesStart, DateOnly seriesEnd, string? color, string? notes,
        bool isBlocker, int? memberId, bool showTitlePublic)
    {
        Title = title;
        Wochentag = wochentag;
        StartTime = startTime;
        EndTime = endTime;
        SeriesStart = seriesStart;
        SeriesEnd = seriesEnd;
        Color = color;
        Notes = notes;
        IsBlocker = isBlocker;
        MemberId = memberId;
        ShowTitlePublic = showTitlePublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public IReadOnlyList<(DateOnly Date, TimeSlot Slot)> GenerateOccurrences()
    {
        var result = new List<(DateOnly, TimeSlot)>();
        var current = SeriesStart;
        while (current <= SeriesEnd)
        {
            if (current.DayOfWeek == Wochentag)
            {
                var startLocal = current.ToDateTime(StartTime);
                var endLocal = current.ToDateTime(EndTime);
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, Zurich);
                var endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, Zurich);
                result.Add((current, new TimeSlot(startUtc, endUtc)));
            }
            current = current.AddDays(1);
        }
        return result;
    }
}
