using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class SetPasswordUseCase(IMemberManagerPort members)
{
    public async Task ExecuteAsync(int memberId, string newPassword)
    {
        if (newPassword.Length < 8)
            throw new DomainException("Das Passwort muss mindestens 8 Zeichen lang sein.");
        await members.AddOrChangePasswordAsync(memberId, newPassword);
    }
}
