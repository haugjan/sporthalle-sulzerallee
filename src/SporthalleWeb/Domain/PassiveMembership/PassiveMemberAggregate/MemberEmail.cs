namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public record MemberEmail
{
    public string Value { get; }

    public MemberEmail(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new DomainException("Ungültige E-Mail-Adresse.");
        Value = value.Trim().ToLowerInvariant();
    }
}
