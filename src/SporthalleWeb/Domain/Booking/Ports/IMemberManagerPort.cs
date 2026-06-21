using SporthalleWeb.Application.Booking;

namespace SporthalleWeb.Domain.Booking.Ports;

public interface IMemberManagerPort
{
    Task<HallMember?> FindByEmailAsync(string email);
    Task<HallMember?> FindByIdAsync(int memberId);
    Task<HallMember> CreateAsync(RegisterRenterCommand cmd, string? password);
    Task UpdateProfileAsync(int memberId, string? name,
        string contactFirstName, string contactLastName,
        string billingAddress, string? addressLine2,
        string billingPostalCode, string billingCity, string? phone);
    Task<IReadOnlyList<HallMember>> SearchAsync(string query);

    Task<bool> CheckPasswordAsync(string email, string password);
    Task SignInAsync(int memberId);
    Task SignOutAsync();
    Task AddOrChangePasswordAsync(int memberId, string newPassword);

    Task<string> GeneratePasswordResetTokenAsync(int memberId);
    Task ResetPasswordAsync(int memberId, string token, string newPassword);

    Task<DateTime?> GetMagicLinkSentAtAsync(int memberId);
    Task SetMagicLinkSentAtAsync(int memberId, DateTime sentAt);
    Task<DateTime?> GetPasswordResetSentAtAsync(int memberId);
    Task SetPasswordResetSentAtAsync(int memberId, DateTime sentAt);
}
