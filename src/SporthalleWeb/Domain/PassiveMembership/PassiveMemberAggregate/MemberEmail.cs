using System.Net.Mail;

namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public record MemberEmail
{
    public string Value { get; }

    public MemberEmail(string value)
    {
        var trimmed = value?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmed)
            || !MailAddress.TryCreate(trimmed, out var parsed)
            || parsed.Address != trimmed)
            throw new DomainException("Ungültige E-Mail-Adresse.");
        Value = trimmed.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
