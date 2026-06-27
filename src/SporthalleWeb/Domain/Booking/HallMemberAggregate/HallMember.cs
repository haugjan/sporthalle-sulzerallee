namespace SporthalleWeb.Domain.Booking.HallMemberAggregate;

public sealed record HallMember(
    int Id,
    RenterEmail Email,
    RenterType RenterType,
    string? Name,
    string ContactFirstName,
    string ContactLastName,
    string BillingAddress,
    string? AddressLine2,
    PostalCode BillingPostalCode,
    string BillingCity,
    string BillingCountry,
    string? Phone,
    string? Notes,
    string? Color,
    bool HasKey
);
