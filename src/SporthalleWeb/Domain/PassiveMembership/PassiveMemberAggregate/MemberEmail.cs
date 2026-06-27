using System.Net.Mail;

namespace SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

public record MemberEmail
{
    public string Value { get; }

    public MemberEmail(string value)
    {
        var trimmed = value?.Trim() ?? "";
        // MailAddress.TryCreate verifies basic structure (local@domain) without being so strict
        // that it rejects addresses already stored from the previous '@'-only check.
        if (string.IsNullOrWhiteSpace(trimmed)
            || !MailAddress.TryCreate(trimmed, out var parsed)
            || parsed.Address != trimmed)
            throw new DomainException("Ungültige E-Mail-Adresse.");
        Value = trimmed.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
