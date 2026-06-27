using SporthalleWeb.Features.Booking;
using Xunit;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class BookingSlotTests
{
    private static TimeSlot MakeSlot() =>
        new(new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void CreateBlocker_SetsTypeAndFields()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "Maintenance", "#ff0000", "note", "admin");
        Assert.Equal(SlotType.Blocker, slot.Type);
        Assert.Equal("Maintenance", slot.Title);
        Assert.Equal("#ff0000", slot.Color);
        Assert.Equal("note", slot.Notes);
        Assert.Equal("admin", slot.CreatedBy);
        Assert.Null(slot.MemberId);
    }

    [Fact]
    public void CreateReserved_SetsTypeAndMemberId()
    {
        var slot = BookingSlot.CreateReserved(42, MakeSlot(), "Training", null, null, "system");
        Assert.Equal(SlotType.Reserved, slot.Type);
        Assert.Equal(42, slot.MemberId);
    }

    [Fact]
    public void CreateBooked_SetsTypeAndMemberId()
    {
        var slot = BookingSlot.CreateBooked(7, MakeSlot(), "Match", null, null, "admin");
        Assert.Equal(SlotType.Booked, slot.Type);
        Assert.Equal(7, slot.MemberId);
    }

    [Fact]
    public void CreateSerie_SetsTypeAndRecurringSlotId()
    {
        var slot = BookingSlot.CreateSerie(MakeSlot(), "Wochentraining", "#c00", null, "system", 99);
        Assert.Equal(SlotType.Recurring, slot.Type);
        Assert.Equal(99, slot.RecurringSlotId);
    }

    [Fact]
    public void Confirm_FromReserved_ChangesToBooked()
    {
        var slot = BookingSlot.CreateReserved(1, MakeSlot(), "T", null, null, "s");
        slot.Confirm();
        Assert.Equal(SlotType.Booked, slot.Type);
    }

    [Fact]
    public void Confirm_NotFromReserved_Throws()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "B", null, null, "admin");
        Assert.Throws<DomainException>(() => slot.Confirm());
    }

    [Fact]
    public void Reject_FromReserved_ChangesToRejected()
    {
        var slot = BookingSlot.CreateReserved(1, MakeSlot(), "T", null, null, "s");
        slot.Reject();
        Assert.Equal(SlotType.Rejected, slot.Type);
    }

    [Fact]
    public void Reject_NotFromReserved_Throws()
    {
        var slot = BookingSlot.CreateBooked(1, MakeSlot(), "T", null, null, "s");
        Assert.Throws<DomainException>(() => slot.Reject());
    }

    [Fact]
    public void Reactivate_FromRejected_ChangesToBooked()
    {
        var slot = BookingSlot.CreateReserved(1, MakeSlot(), "T", null, null, "s");
        slot.Reject();
        slot.Reactivate();
        Assert.Equal(SlotType.Booked, slot.Type);
    }

    [Fact]
    public void Reactivate_NotFromRejected_Throws()
    {
        var slot = BookingSlot.CreateReserved(1, MakeSlot(), "T", null, null, "s");
        Assert.Throws<DomainException>(() => slot.Reactivate());
    }

    [Fact]
    public void Update_SetsNewValues()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "Old", "#000", "old note", "admin");
        slot.Update("New", "#fff", "new note", showTitlePublic: false);
        Assert.Equal("New", slot.Title);
        Assert.Equal("#fff", slot.Color);
        Assert.Equal("new note", slot.Notes);
    }

    [Fact]
    public void ShowTitlePublic_DefaultIsFalse()
    {
        var blocker = BookingSlot.CreateBlocker(MakeSlot(), "B", null, null, "admin");
        var reserved = BookingSlot.CreateReserved(1, MakeSlot(), "R", null, null, "admin");
        var booked = BookingSlot.CreateBooked(2, MakeSlot(), "B2", null, null, "admin");
        Assert.False(blocker.ShowTitlePublic);
        Assert.False(reserved.ShowTitlePublic);
        Assert.False(booked.ShowTitlePublic);
    }

    [Fact]
    public void CreateBlocker_WithShowTitlePublicTrue_SetsFlag()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "T", null, null, "admin", showTitlePublic: true);
        Assert.True(slot.ShowTitlePublic);
    }

    [Fact]
    public void Update_WithShowTitlePublicTrue_SetsFlag()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "T", null, null, "admin");
        slot.Update("T", null, null, showTitlePublic: true);
        Assert.True(slot.ShowTitlePublic);
    }

    [Fact]
    public void Update_ClearsShowTitlePublicWhenFalse()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "T", null, null, "admin", showTitlePublic: true);
        slot.Update("T", null, null, showTitlePublic: false);
        Assert.False(slot.ShowTitlePublic);
    }

    [Fact]
    public void FromPersistence_WithShowTitlePublicTrue_RestoresIt()
    {
        var start = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var slot = BookingSlot.FromPersistence(1, null, "Blocker", start, end, "T", null, null, now, now, "admin", showTitlePublic: true);
        Assert.True(slot.ShowTitlePublic);
    }

    [Fact]
    public void FromPersistence_WithShowTitlePublicFalse_DefaultsFalse()
    {
        var start = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var slot = BookingSlot.FromPersistence(1, null, "Blocker", start, end, "T", null, null, now, now, "admin");
        Assert.False(slot.ShowTitlePublic);
    }

    [Fact]
    public void Reschedule_SetsNewSlot()
    {
        var slot = BookingSlot.CreateBlocker(MakeSlot(), "B", null, null, "admin");
        var newSlot = new TimeSlot(
            new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 2, 11, 0, 0, DateTimeKind.Utc));
        slot.Reschedule(newSlot);
        Assert.Equal(newSlot, slot.Slot);
    }

    [Fact]
    public void Reassign_UpdatesMemberId()
    {
        var slot = BookingSlot.CreateReserved(1, MakeSlot(), "T", null, null, "s");
        slot.Reassign(99);
        Assert.Equal(99, slot.MemberId);
    }

    [Fact]
    public void FromPersistence_RestoresAllFields()
    {
        var start = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var slot = BookingSlot.FromPersistence(
            1, 42, "Booked", start, end, "Title", "#abc", "Notes", created, created, "admin", 7);
        Assert.Equal(1, slot.Id);
        Assert.Equal(42, slot.MemberId);
        Assert.Equal(SlotType.Booked, slot.Type);
        Assert.Equal("Title", slot.Title);
        Assert.Equal(7, slot.RecurringSlotId);
    }
}
