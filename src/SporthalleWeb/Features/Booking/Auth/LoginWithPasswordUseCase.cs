using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Auth;

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
