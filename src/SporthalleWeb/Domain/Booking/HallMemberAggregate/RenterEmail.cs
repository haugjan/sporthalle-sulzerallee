namespace SporthalleWeb.Domain.Booking;

public record RenterEmail
{
    public string Value { get; }

    public RenterEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new DomainException("Ungültige E-Mail-Adresse.");
        Value = value.Trim().ToLowerInvariant();
    }
}
