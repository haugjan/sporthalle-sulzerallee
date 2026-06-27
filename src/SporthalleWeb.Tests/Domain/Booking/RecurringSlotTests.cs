using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.RecurringAggregate;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class RecurringSlotTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var start = new DateOnly(2026, 1, 5);  // Monday
        var end = new DateOnly(2026, 1, 26);   // Monday
        var slot = RecurringSlot.Create(
            "Training", DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(11, 0),
            start, end, "notes", "admin");

        Assert.Equal("Training", slot.Title);
        Assert.Equal(DayOfWeek.Monday, slot.Weekday);
        Assert.Equal(new TimeOnly(9, 0), slot.StartTime);
        Assert.Equal(new TimeOnly(11, 0), slot.EndTime);
        Assert.Equal(start, slot.SeriesStart);
        Assert.Equal(end, slot.SeriesEnd);
        Assert.Equal("notes", slot.Notes);
        Assert.Equal("admin", slot.CreatedBy);
    }

    [Fact]
    public void GenerateOccurrences_ReturnsOnlyMatchingDayOfWeek()
    {
        // 4 Mondays: Jan 5, 12, 19, 26
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31),
            null, "admin");

        var occurrences = slot.GenerateOccurrences();
        Assert.Equal(4, occurrences.Count);
        Assert.All(occurrences, o => Assert.Equal(DayOfWeek.Monday, o.Date.DayOfWeek));
    }

    [Fact]
    public void GenerateOccurrences_StartDateIsMatchingDay_Included()
    {
        var monday = new DateOnly(2026, 1, 5);
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday,
            new TimeOnly(10, 0), new TimeOnly(12, 0),
            monday, monday, null, "admin");

        var occurrences = slot.GenerateOccurrences();
        Assert.Single(occurrences);
        Assert.Equal(monday, occurrences[0].Date);
    }

    [Fact]
    public void GenerateOccurrences_NoDayOfWeekInRange_ReturnsEmpty()
    {
        // Jan 6 (Tue) to Jan 7 (Wed) — no Monday
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday,
            new TimeOnly(10, 0), new TimeOnly(12, 0),
            new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 7),
            null, "admin");

        Assert.Empty(slot.GenerateOccurrences());
    }

    [Fact]
    public void GenerateOccurrences_SlotsAreUtc()
    {
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday,
            new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5),
            null, "admin");

        var (_, timeSlot) = slot.GenerateOccurrences()[0];
        Assert.Equal(DateTimeKind.Utc, timeSlot.StartUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, timeSlot.EndUtc.Kind);
    }

    [Fact]
    public void Update_ChangesAllFields()
    {
        var slot = RecurringSlot.Create(
            "Old", DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 26), null, "admin");

        slot.Update("New", DayOfWeek.Friday, new TimeOnly(14, 0), new TimeOnly(16, 0),
            new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), "updated",
            isBlocker: false, memberId: null, showTitlePublic: false);

        Assert.Equal("New", slot.Title);
        Assert.Equal(DayOfWeek.Friday, slot.Weekday);
        Assert.Equal(new TimeOnly(14, 0), slot.StartTime);
    }

    [Fact]
    public void ShowTitlePublic_DefaultIsFalse()
    {
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), null, "admin");
        Assert.False(slot.ShowTitlePublic);
    }

    [Fact]
    public void Create_WithShowTitlePublicTrue_SetsFlag()
    {
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), null, "admin",
            showTitlePublic: true);
        Assert.True(slot.ShowTitlePublic);
    }

    [Fact]
    public void Update_SetsShowTitlePublic()
    {
        var slot = RecurringSlot.Create(
            "T", DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 26), null, "admin");

        slot.Update("T", DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0),
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 26), null,
            isBlocker: false, memberId: null, showTitlePublic: true);

        Assert.True(slot.ShowTitlePublic);
    }

    [Fact]
    public void FromPersistence_RestoresShowTitlePublic()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var slot = RecurringSlot.FromPersistence(
            1, "T", 1, "09:00", "11:00", "2026-01-05", "2026-01-26",
            null, "admin", now, now, showTitlePublic: true);
        Assert.True(slot.ShowTitlePublic);
    }

    [Fact]
    public void FromPersistence_ShowTitlePublicDefaultsFalse()
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var slot = RecurringSlot.FromPersistence(
            1, "T", 1, "09:00", "11:00", "2026-01-05", "2026-01-26",
            null, "admin", now, now);
        Assert.False(slot.ShowTitlePublic);
    }
}
