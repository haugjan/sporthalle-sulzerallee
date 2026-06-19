namespace SporthalleWeb.Domain.Reservierung;

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
