using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Features.Booking.Requests;

namespace SporthalleWeb.Features.Booking.Ports;

public interface IHallMembers
{
    Task<HallMember?> FindByEmailAsync(string email);
    Task<HallMember?> FindByIdAsync(int memberId);
    Task<HallMember> CreateAsync(RegisterRenterCommand cmd);
    Task UpdateProfileAsync(int memberId, string? name,
        string contactFirstName, string contactLastName,
        string billingAddress, string? addressLine2,
        string billingPostalCode, string billingCity, string? phone);
    Task<IReadOnlyList<HallMember>> SearchAsync(string query);
}
