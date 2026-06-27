namespace SporthalleWeb.Domain.Booking.HallMemberAggregate;

public sealed record HallMember(
    int Id,
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
    string? Notes,
    bool HasKey
);
