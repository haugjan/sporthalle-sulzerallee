using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class LoginWithPassword(IHallMembers members)
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
