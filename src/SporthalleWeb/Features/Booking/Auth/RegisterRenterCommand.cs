using SporthalleWeb.Features.Booking;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed record RegisterRenterCommand(
    string Email,
    RenterType RenterType,
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
    string? Password
);
