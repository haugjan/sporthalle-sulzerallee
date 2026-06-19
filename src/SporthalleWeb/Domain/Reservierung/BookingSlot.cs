namespace SporthalleWeb.Domain.Reservierung;

public sealed class BookingSlot
{
    public int Id { get; private set; }
    public int? MemberId { get; private set; }
    public int? RecurringRuleId { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Provisional;
    public TimeSlot Slot { get; private set; } = null!;
    public decimal? PricePerBlock { get; private set; }
    public int? TotalBlocks { get; private set; }
    public decimal? TotalPrice { get; private set; }
    public string? PriceNote { get; private set; }
    public bool IsRecurringSlot { get; private set; }
    public string? Color { get; private set; }
    public string? EventType { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "";

    private BookingSlot() { }

    public static BookingSlot CreateUserBooking(
        int memberId, TimeSlot slot, decimal pricePerBlock,
        string eventType, string? notes, string createdBy)
    {
        var blocks = slot.BlockCount();
        return new BookingSlot
        {
            MemberId = memberId,
            Status = BookingStatus.Provisional,
            Slot = slot,
            PricePerBlock = pricePerBlock,
            TotalBlocks = blocks,
            TotalPrice = pricePerBlock * blocks,
            IsRecurringSlot = false,
            EventType = eventType,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public static BookingSlot CreateRecurringSlot(
        int? memberId, int recurringRuleId, TimeSlot slot, string? color, string createdBy) =>
        new()
        {
            MemberId = memberId,
            RecurringRuleId = recurringRuleId,
            Status = BookingStatus.Confirmed,
            Slot = slot,
            IsRecurringSlot = true,
            Color = color,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public static BookingSlot FromPersistence(
        int id, int? memberId, int? recurringRuleId, string status,
        DateTime startUtc, DateTime endUtc,
        decimal? pricePerBlock, int? totalBlocks, decimal? totalPrice, string? priceNote,
        bool isRecurringSlot, string? color, string? eventType, string? notes,
        DateTime createdAt, DateTime updatedAt, string createdBy) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            RecurringRuleId = recurringRuleId,
            Status = BookingStatus.FromString(status),
            Slot = new TimeSlot(startUtc, endUtc),
            PricePerBlock = pricePerBlock,
            TotalBlocks = totalBlocks,
            TotalPrice = totalPrice,
            PriceNote = priceNote,
            IsRecurringSlot = isRecurringSlot,
            Color = color,
            EventType = eventType,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedBy = createdBy
        };

    public void Confirm()
    {
        if (Status != BookingStatus.Provisional)
            throw new DomainException("Nur provisorische Buchungen können bestätigt werden.");
        Status = BookingStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != BookingStatus.Provisional)
            throw new DomainException("Nur provisorische Buchungen können abgelehnt werden.");
        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Buchung ist bereits storniert.");
        Status = BookingStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustPrice(decimal newPricePerBlock, string? note)
    {
        if (IsRecurringSlot)
            throw new DomainException("Serienbuchungen haben keinen Einzelpreis.");
        PricePerBlock = newPricePerBlock;
        TotalPrice = newPricePerBlock * (TotalBlocks ?? 0);
        PriceNote = note;
        UpdatedAt = DateTime.UtcNow;
    }
}
