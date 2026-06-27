using SporthalleWeb.Domain.Booking.HallMemberAggregate;

namespace SporthalleWeb.Features.Booking.Dtos;

public sealed record HallMemberDto(
    int Id,
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
    bool HasPassword)
{
    public static HallMemberDto From(HallMember m) => new(
        m.Id, m.Email, m.RenterType.Value.ToString(),
        m.Name, m.ContactFirstName, m.ContactLastName,
        m.BillingAddress, m.AddressLine2, m.BillingPostalCode, m.BillingCity, m.BillingCountry,
        m.Phone, m.HasKey, m.HasPassword);
}
