using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class SetPassword(IHallMembers members)
{
    public async Task ExecuteAsync(int memberId, string newPassword)
    {
        if (newPassword.Length < 8)
            throw new DomainException("Das Passwort muss mindestens 8 Zeichen lang sein.");
        await members.AddOrChangePasswordAsync(memberId, newPassword);
    }
}
