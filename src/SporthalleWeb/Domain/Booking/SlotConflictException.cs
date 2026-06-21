namespace SporthalleWeb.Domain.Booking;

public class SlotConflictException(TimeSlot requested, IReadOnlyList<BookingSlot> conflicts)
    : DomainException("Dieser Zeitslot ist bereits belegt.")
{
    public TimeSlot Requested { get; } = requested;
    public IReadOnlyList<BookingSlot> Conflicts { get; } = conflicts;
}
