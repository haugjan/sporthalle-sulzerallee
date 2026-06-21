namespace SporthalleWeb.Presentation.Booking.Dtos;

public sealed record RegisterRenterRequest(
    string Email,
    string ContactPerson,
    string RenterType,
    string BillingName,
    string BillingAddress,
    string BillingPostalCode,
    string BillingCity,
    string BillingCountry,
    string? Phone,
    bool HasKey,
    string? Password,
    string? CaptchaToken);
