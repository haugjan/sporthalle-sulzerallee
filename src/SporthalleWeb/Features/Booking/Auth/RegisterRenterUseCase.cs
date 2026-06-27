using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Auth;

public sealed class RegisterRenter(
    IHallMembers members,
    IMagicLinkTokens tokenRepo,
    ICaptcha captcha,
    IBookingEmail email)
{
    public async Task ExecuteAsync(RegisterRenterCommand cmd, string? captchaToken, string? remoteIp)
    {
        if (!await captcha.VerifyAsync(captchaToken, remoteIp))
            throw new DomainException("CAPTCHA-Überprüfung fehlgeschlagen.");

        // Umbraco rejects members with an empty Name ("All variants must have a name").
        // Name is derived from ContactFirstName + ContactLastName, so at least one must be present.
        if (string.IsNullOrWhiteSpace(cmd.ContactFirstName) && string.IsNullOrWhiteSpace(cmd.ContactLastName))
            throw new DomainException("Vor- oder Nachname ist erforderlich.");

        if (await members.FindByEmailAsync(cmd.Email) is not null)
            throw new DomainException("Diese E-Mail-Adresse ist bereits registriert.");

        var member = await members.CreateAsync(cmd, cmd.Password);

        var (plainToken, tokenHash) = SendMagicLink.GenerateToken();
        var magicLink = $"https://www.sporthalle-sulzerallee.ch/reservierung/auth/validate?token={plainToken}";
        await tokenRepo.SaveAsync(MagicLinkToken.Create(member.Id, tokenHash, remoteIp));

        await email.SendRegistrationConfirmationWithMagicLinkAsync(member, magicLink);
    }
}
