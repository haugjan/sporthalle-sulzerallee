using SporthalleWeb.Domain.Reservierung;

namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record HallMemberDto(
    int Id,
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
    bool HasPassword)
{
    public static HallMemberDto From(HallMember m) => new(
        m.Id, m.Email, m.ContactPerson,
        m.RenterType.Value.ToString(),
        m.BillingName, m.BillingAddress, m.BillingPostalCode, m.BillingCity, m.BillingCountry,
        m.Phone, m.HasKey, m.HasPassword);
}
