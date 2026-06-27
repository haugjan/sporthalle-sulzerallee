using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Auth;

public sealed class SetPassword(IHallMembers members)
{
    public async Task ExecuteAsync(int memberId, string newPassword)
    {
        if (newPassword.Length < 8)
            throw new DomainException("Das Passwort muss mindestens 8 Zeichen lang sein.");
        await members.AddOrChangePasswordAsync(memberId, newPassword);
    }
}
