using SporthalleWeb.Domain.Reservierung;

namespace SporthalleWeb.Application.Reservierung;

public sealed record RegisterRenterCommand(
    string Email,
    string ContactPerson,
    RenterType RenterType,
    string BillingName,
    string BillingAddress,
    string BillingPostalCode,
    string BillingCity,
    string BillingCountry,
    string? Phone,
    bool HasKey
);
