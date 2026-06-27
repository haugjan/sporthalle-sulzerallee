using System.Globalization;
using Microsoft.Data.Sqlite;
using NPoco;
using SporthalleWeb.Infrastructure.Booking;
using Xunit;

namespace SporthalleWeb.Tests.Infrastructure.Booking;

/// <summary>
/// Guards the NPoco ↔ SQLite mapping for RecurringSlotRecord. The date/time fields are
/// stored as TEXT ("HH:mm" / "yyyy-MM-dd") because NPoco + Microsoft.Data.Sqlite cannot
/// cast a TEXT column to TimeOnly/DateOnly directly. RecurringSlotRepository is responsible
/// for parsing the strings into domain types.
/// </summary>
public sealed class RecurringSlotDateTimeMappingTests
{
    private static IDatabase OpenDb(SqliteConnection conn) => new Database(conn, DatabaseType.SQLite);

    [Fact]
    public void Legacy_text_values_are_read_as_strings_and_parse_correctly()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = OpenDb(conn);

        db.Execute(@"CREATE TABLE RecurringSlots (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Wochentag INTEGER NOT NULL,
            StartTime TEXT NOT NULL, EndTime TEXT NOT NULL, SeriesStart TEXT NOT NULL, SeriesEnd TEXT NOT NULL,
            Notes TEXT NULL, IsBlocker INTEGER NOT NULL, MemberId INTEGER NULL, CreatedBy TEXT NOT NULL,
            CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, IsDeleted INTEGER NOT NULL, ShowTitlePublic INTEGER NOT NULL)");
        db.Execute(@"INSERT INTO RecurringSlots
            (Title, Wochentag, StartTime, EndTime, SeriesStart, SeriesEnd, Notes, IsBlocker, MemberId, CreatedBy, CreatedAt, UpdatedAt, IsDeleted, ShowTitlePublic)
            VALUES ('Training', 1, '09:00', '11:00', '2026-01-05', '2026-01-26', NULL, 0, NULL, 'admin', '2026-01-01 00:00:00', '2026-01-01 00:00:00', 0, 0)");

        var record = db.Single<RecurringSlotRecord>("SELECT * FROM RecurringSlots");

        Assert.Equal(new TimeOnly(9, 0),  TimeOnly.Parse(record.StartTime, CultureInfo.InvariantCulture));
        Assert.Equal(new TimeOnly(11, 0), TimeOnly.Parse(record.EndTime, CultureInfo.InvariantCulture));
        Assert.Equal(new DateOnly(2026, 1, 5),  DateOnly.Parse(record.SeriesStart, CultureInfo.InvariantCulture));
        Assert.Equal(new DateOnly(2026, 1, 26), DateOnly.Parse(record.SeriesEnd, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Inserts_and_reads_back_time_and_date_as_formatted_strings()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = OpenDb(conn);

        db.Execute(@"CREATE TABLE RecurringSlots (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Wochentag INTEGER NOT NULL,
            StartTime TEXT NOT NULL, EndTime TEXT NOT NULL, SeriesStart TEXT NOT NULL, SeriesEnd TEXT NOT NULL,
            Notes TEXT NULL, IsBlocker INTEGER NOT NULL, MemberId INTEGER NULL, CreatedBy TEXT NOT NULL,
            CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, IsDeleted INTEGER NOT NULL, ShowTitlePublic INTEGER NOT NULL)");

        var startTime   = new TimeOnly(18, 30);
        var endTime     = new TimeOnly(20, 0);
        var seriesStart = new DateOnly(2026, 3, 1);
        var seriesEnd   = new DateOnly(2026, 6, 30);

        db.Insert(new RecurringSlotRecord
        {
            Title       = "Training",
            Weekday     = 2,
            StartTime   = startTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            EndTime     = endTime.ToString("HH:mm", CultureInfo.InvariantCulture),
            SeriesStart = seriesStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            SeriesEnd   = seriesEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IsBlocker   = false,
            CreatedBy   = "admin",
            CreatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });

        var read = db.Single<RecurringSlotRecord>("SELECT * FROM RecurringSlots");
        Assert.Equal(startTime,   TimeOnly.Parse(read.StartTime, CultureInfo.InvariantCulture));
        Assert.Equal(endTime,     TimeOnly.Parse(read.EndTime, CultureInfo.InvariantCulture));
        Assert.Equal(seriesStart, DateOnly.Parse(read.SeriesStart, CultureInfo.InvariantCulture));
        Assert.Equal(seriesEnd,   DateOnly.Parse(read.SeriesEnd, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Date_comparison_in_sql_works_with_DateOnly_parameter()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var db = OpenDb(conn);

        db.Execute(@"CREATE TABLE RecurringSlots (
            Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL, Wochentag INTEGER NOT NULL,
            StartTime TEXT NOT NULL, EndTime TEXT NOT NULL, SeriesStart TEXT NOT NULL, SeriesEnd TEXT NOT NULL,
            Notes TEXT NULL, IsBlocker INTEGER NOT NULL, MemberId INTEGER NULL, CreatedBy TEXT NOT NULL,
            CreatedAt TEXT NOT NULL, UpdatedAt TEXT NOT NULL, IsDeleted INTEGER NOT NULL, ShowTitlePublic INTEGER NOT NULL)");
        db.Execute(@"INSERT INTO RecurringSlots
            (Title, Wochentag, StartTime, EndTime, SeriesStart, SeriesEnd, Notes, IsBlocker, MemberId, CreatedBy, CreatedAt, UpdatedAt, IsDeleted, ShowTitlePublic)
            VALUES ('Training', 1, '09:00', '11:00', '2026-01-05', '2026-01-26', NULL, 0, NULL, 'admin', '2026-01-01 00:00:00', '2026-01-01 00:00:00', 0, 0)");

        var hit = db.Fetch<RecurringSlotRecord>(
            new Sql("SELECT * FROM RecurringSlots WHERE SeriesEnd >= @0 AND SeriesStart <= @1",
                new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));

        Assert.Single(hit);
    }
}
