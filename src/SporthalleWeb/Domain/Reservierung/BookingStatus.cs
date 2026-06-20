namespace SporthalleWeb.Domain.Reservierung;

public enum BookingStatusValue { Provisorisch, Bestätigt, Storniert }

public record BookingStatus
{
    public BookingStatusValue Value { get; }

    public static readonly BookingStatus Provisional = new(BookingStatusValue.Provisorisch);
    public static readonly BookingStatus Confirmed   = new(BookingStatusValue.Bestätigt);
    public static readonly BookingStatus Cancelled   = new(BookingStatusValue.Storniert);

    private BookingStatus(BookingStatusValue v) => Value = v;

    public static BookingStatus FromString(string s) => s switch
    {
        "Provisorisch" => Provisional,
        "Bestätigt"    => Confirmed,
        "Storniert"    => Cancelled,
        _              => throw new DomainException($"Unbekannter Buchungsstatus: {s}")
    };

    public override string ToString() => Value.ToString();
}
