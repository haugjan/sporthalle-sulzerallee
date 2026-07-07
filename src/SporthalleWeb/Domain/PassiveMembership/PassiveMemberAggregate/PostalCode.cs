namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public sealed record PostalCode
{
    public string Value { get; }

    private PostalCode(string value) => Value = value;

    public static PostalCode Create(string value, string? country = null)
    {
        var trimmed = value?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new DomainException("PLZ ist erforderlich.");

        if (IsSwiss(country))
        {
            if (trimmed.Length != 4 || !trimmed.All(char.IsDigit)
                || int.Parse(trimmed) is < 1000 or > 9999)
                throw new DomainException("Schweizer PLZ muss 4-stellig sein (1000–9999).");
        }
        else if (trimmed.Length > 10)
        {
            throw new DomainException("Ungültige PLZ.");
        }

        return new PostalCode(trimmed);
    }

    public static PostalCode FromPersistence(string? value) => new((value ?? "").Trim());

    private static bool IsSwiss(string? country)
    {
        var c = country?.Trim();
        return string.IsNullOrEmpty(c)
            || c.Equals("Schweiz", StringComparison.OrdinalIgnoreCase)
            || c.Equals("Switzerland", StringComparison.OrdinalIgnoreCase)
            || c.Equals("CH", StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => Value;
}
