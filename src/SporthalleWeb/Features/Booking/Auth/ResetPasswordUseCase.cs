using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class ResetPassword(IHallMembers members)
{
    public async Task ExecuteAsync(int memberId, string token, string newPassword)
    {
        if (newPassword.Length < 8)
            throw new DomainException("Das Passwort muss mindestens 8 Zeichen lang sein.");
        await members.ResetPasswordAsync(memberId, token, newPassword);
    }
}
