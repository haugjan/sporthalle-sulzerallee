namespace SporthalleWeb.Features.Booking;

public sealed class BookingSlot
{
    public int Id { get; private set; }
    public int? MemberId { get; private set; }
    public SlotType Type { get; private set; }
    public TimeSlot Slot { get; private set; } = null!;
    public string Title { get; private set; } = "";
    public string? Color { get; private set; }
    public string? Notes { get; private set; }
    public bool ShowTitlePublic { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "";
    public int? RecurringSlotId { get; private set; }

    private BookingSlot() { }

    public static BookingSlot CreateBlocker(
        TimeSlot slot, string title, string? color, string? notes, string createdBy,
        bool showTitlePublic = false) =>
        new()
        {
            Type = SlotType.Blocker,
            Slot = slot,
            Title = title,
            Color = color,
            Notes = notes,
            ShowTitlePublic = showTitlePublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public static BookingSlot CreateReserved(
        int memberId, TimeSlot slot, string title, string? color, string? notes, string createdBy,
        bool showTitlePublic = false) =>
        new()
        {
            MemberId = memberId,
            Type = SlotType.Reserved,
            Slot = slot,
            Title = title,
            Color = color,
            Notes = notes,
            ShowTitlePublic = showTitlePublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public static BookingSlot CreateBooked(
        int memberId, TimeSlot slot, string title, string? color, string? notes, string createdBy,
        bool showTitlePublic = false) =>
        new()
        {
            MemberId = memberId,
            Type = SlotType.Booked,
            Slot = slot,
            Title = title,
            Color = color,
            Notes = notes,
            ShowTitlePublic = showTitlePublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public static BookingSlot CreateSerie(
        TimeSlot slot, string title, string? color, string? notes, string createdBy, int recurringSlotId,
        SlotType type = SlotType.Recurring, int? memberId = null, bool showTitlePublic = false) =>
        new()
        {
            Type = type,
            MemberId = memberId,
            Slot = slot,
            Title = title,
            Color = color,
            Notes = notes,
            ShowTitlePublic = showTitlePublic,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            RecurringSlotId = recurringSlotId
        };

    public static BookingSlot FromPersistence(
        int id, int? memberId, string type,
        DateTime startUtc, DateTime endUtc,
        string title, string? color, string? notes,
        DateTime createdAt, DateTime updatedAt, string createdBy,
        int? recurringSlotId = null, bool showTitlePublic = false) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            Type = Enum.Parse<SlotType>(type),
            Slot = new TimeSlot(startUtc, endUtc),
            Title = title,
            Color = color,
            Notes = notes,
            ShowTitlePublic = showTitlePublic,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedBy = createdBy,
            RecurringSlotId = recurringSlotId
        };

    public void Confirm()
    {
        if (Type != SlotType.Reserved)
            throw new DomainException("Nur reservierte Buchungen können bestätigt werden.");
        Type = SlotType.Booked;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Type != SlotType.Reserved)
            throw new DomainException("Nur reservierte Buchungen können abgelehnt werden.");
        Type = SlotType.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (Type != SlotType.Rejected)
            throw new DomainException("Nur abgelehnte Buchungen können reaktiviert werden.");
        Type = SlotType.Booked;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string title, string? color, string? notes, bool showTitlePublic)
    {
        Title = title;
        Color = color;
        Notes = notes;
        ShowTitlePublic = showTitlePublic;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(TimeSlot newSlot)
    {
        Slot = newSlot;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reassign(int? newMemberId)
    {
        MemberId = newMemberId;
        UpdatedAt = DateTime.UtcNow;
    }
}
