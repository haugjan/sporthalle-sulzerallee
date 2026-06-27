using System.Net.Mail;
using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Domain.Booking.HallMemberAggregate;

public record RenterEmail
{
    public string Value { get; }

    public RenterEmail(string value)
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
