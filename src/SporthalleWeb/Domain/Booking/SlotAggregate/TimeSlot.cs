namespace SporthalleWeb.Domain.Booking.SlotAggregate;

public record TimeSlot
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public TimeSlot(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
            throw new DomainException("TimeSlot muss UTC-Werte enthalten.");
        if (endUtc <= startUtc)
            throw new DomainException("EndUtc muss nach StartUtc liegen.");
        if ((endUtc - startUtc).TotalMinutes < 30)
            throw new DomainException("Mindestbuchungsdauer beträgt 30 Minuten.");
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public int BlockCount(int blockMinutes = 30) =>
        (int)((EndUtc - StartUtc).TotalMinutes / blockMinutes);

    public bool OverlapsWith(TimeSlot other) =>
        StartUtc < other.EndUtc && EndUtc > other.StartUtc;
}
