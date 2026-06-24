using SporthalleWeb.Domain.Booking;
using Xunit;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class TimeSlotTests
{
    private static DateTime Utc(int hour, int minute = 0) =>
        new(2026, 6, 1, hour, minute, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ValidSlot_SetsProperties()
    {
        var slot = new TimeSlot(Utc(8), Utc(10));
        Assert.Equal(Utc(8), slot.StartUtc);
        Assert.Equal(Utc(10), slot.EndUtc);
    }

    [Fact]
    public void Constructor_NonUtcStart_Throws()
    {
        var local = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Local);
        Assert.Throws<DomainException>(() => new TimeSlot(local, Utc(10)));
    }

    [Fact]
    public void Constructor_NonUtcEnd_Throws()
    {
        var local = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Local);
        Assert.Throws<DomainException>(() => new TimeSlot(Utc(8), local));
    }

    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        Assert.Throws<DomainException>(() => new TimeSlot(Utc(10), Utc(8)));
    }

    [Fact]
    public void Constructor_EndEqualsStart_Throws()
    {
        Assert.Throws<DomainException>(() => new TimeSlot(Utc(8), Utc(8)));
    }

    [Fact]
    public void Constructor_LessThan30Minutes_Throws()
    {
        Assert.Throws<DomainException>(() => new TimeSlot(Utc(8), Utc(8, 29)));
    }

    [Fact]
    public void Constructor_Exactly30Minutes_Succeeds()
    {
        var slot = new TimeSlot(Utc(8), Utc(8, 30));
        Assert.Equal(Utc(8, 30), slot.EndUtc);
    }

    [Theory]
    [InlineData(8, 10, 4)]   // 120 min / 30 = 4 blocks
    [InlineData(8, 9, 2)]    // 60 min / 30 = 2 blocks
    [InlineData(8, 8, 1, 30)]  // 30 min / 30 = 1 block
    public void BlockCount_ReturnsCorrectCount(int startHour, int endHour, int expected, int endMinute = 0)
    {
        var slot = new TimeSlot(Utc(startHour), new DateTime(2026, 6, 1, endHour, endMinute, 0, DateTimeKind.Utc));
        Assert.Equal(expected, slot.BlockCount());
    }

    [Fact]
    public void BlockCount_CustomBlockSize_DividesCorrectly()
    {
        var slot = new TimeSlot(Utc(8), Utc(10)); // 120 min
        Assert.Equal(2, slot.BlockCount(60));
    }

    [Fact]
    public void OverlapsWith_OverlappingSlots_ReturnsTrue()
    {
        var a = new TimeSlot(Utc(8), Utc(10));
        var b = new TimeSlot(Utc(9), Utc(11));
        Assert.True(a.OverlapsWith(b));
        Assert.True(b.OverlapsWith(a));
    }

    [Fact]
    public void OverlapsWith_AdjacentSlots_ReturnsFalse()
    {
        var a = new TimeSlot(Utc(8), Utc(10));
        var b = new TimeSlot(Utc(10), Utc(12));
        Assert.False(a.OverlapsWith(b));
        Assert.False(b.OverlapsWith(a));
    }

    [Fact]
    public void OverlapsWith_NonOverlappingSlots_ReturnsFalse()
    {
        var a = new TimeSlot(Utc(8), Utc(9));
        var b = new TimeSlot(Utc(10), Utc(11));
        Assert.False(a.OverlapsWith(b));
    }

    [Fact]
    public void OverlapsWith_ContainedSlot_ReturnsTrue()
    {
        var outer = new TimeSlot(Utc(8), Utc(12));
        var inner = new TimeSlot(Utc(9), Utc(11));
        Assert.True(outer.OverlapsWith(inner));
        Assert.True(inner.OverlapsWith(outer));
    }
}
