using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class LoginWithPasswordUseCase(IMemberManagerPort members)
{
    public async Task<HallMember> ExecuteAsync(string email, string password)
    {
        var member = await members.FindByEmailAsync(email)
            ?? throw new DomainException("Unbekannte E-Mail-Adresse.");

        if (!await members.CheckPasswordAsync(email, password))
            throw new DomainException("Passwort ungültig.");

        await members.SignInAsync(member.Id);
        return member;
    }
}
