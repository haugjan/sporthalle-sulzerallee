namespace SporthalleWeb.Features.Booking;

public sealed record RegisterRenterRequest(
    string Email,
    string RenterType,
    string? Name,
    string ContactFirstName,
    string ContactLastName,
    string BillingAddress,
    string? AddressLine2,
    string BillingPostalCode,
    string BillingCity,
    string BillingCountry,
    string? Phone,
    bool HasKey,
    string? Password,
    string? CaptchaToken);
