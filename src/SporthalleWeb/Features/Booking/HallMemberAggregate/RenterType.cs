namespace SporthalleWeb.Features.Booking;

public enum RenterTypeValue { Verein, Firma, Privatperson, Schule }

public record RenterType
{
    public RenterTypeValue Value { get; }

    public RenterType(string raw) =>
        Value = Enum.TryParse<RenterTypeValue>(raw, out var v)
            ? v
            : throw new DomainException($"Unbekannter Mietertyp: {raw}");
}
