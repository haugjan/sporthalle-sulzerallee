namespace SporthalleWeb.Domain.Reservierung;

public sealed class BookingSlot
{
    public int Id { get; private set; }
    public int? RenterId { get; private set; }
    public int? RecurringRuleId { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Provisional;
    public TimeSlot Slot { get; private set; } = null!;
    public bool IsRecurringSlot { get; private set; }
    public string? Color { get; private set; }
    public string? EventType { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "";

    private BookingSlot() { }

    public static BookingSlot FromPersistence(
        int id, int? renterId, int? recurringRuleId, string status,
        DateTime startUtc, DateTime endUtc, bool isRecurringSlot,
        string? color, string? eventType, string? notes,
        DateTime createdAt, DateTime updatedAt, string createdBy) =>
        new()
        {
            Id = id,
            RenterId = renterId,
            RecurringRuleId = recurringRuleId,
            Status = BookingStatus.FromString(status),
            Slot = new TimeSlot(startUtc, endUtc),
            IsRecurringSlot = isRecurringSlot,
            Color = color,
            EventType = eventType,
            Notes = notes,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            CreatedBy = createdBy
        };
}
