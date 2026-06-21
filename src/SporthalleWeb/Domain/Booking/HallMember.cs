namespace SporthalleWeb.Domain.Booking;

public sealed record HallMember(
    int Id,
    string Email,
    string ContactPerson,
    RenterType RenterType,
    string BillingName,
    string BillingAddress,
    string BillingPostalCode,
    string BillingCity,
    string BillingCountry,
    string? Phone,
    bool HasKey,
    bool HasPassword,
    DateTime? MagicLinkSentAt,
    DateTime? PasswordResetSentAt
);
