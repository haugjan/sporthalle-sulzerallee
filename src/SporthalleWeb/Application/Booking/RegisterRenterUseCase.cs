using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;
using SporthalleWeb.Domain.Shared;

namespace SporthalleWeb.Application.Booking;

public sealed class RegisterRenterUseCase(
    IMemberManagerPort members,
    IMagicLinkTokenRepository tokenRepo,
    ICaptchaPort captcha,
    IBookingEmailPort email)
{
    public async Task ExecuteAsync(RegisterRenterCommand cmd, string? captchaToken, string? remoteIp)
    {
        if (!await captcha.VerifyAsync(captchaToken, remoteIp))
            throw new DomainException("CAPTCHA-Überprüfung fehlgeschlagen.");

        if (await members.FindByEmailAsync(cmd.Email) is not null)
            throw new DomainException("Diese E-Mail-Adresse ist bereits registriert.");

        var member = await members.CreateAsync(cmd, cmd.Password);

        var (plainToken, tokenHash) = SendMagicLinkUseCase.GenerateToken();
        var magicLink = $"https://www.sporthalle-sulzerallee.ch/reservierung/auth/validate?token={plainToken}";
        await tokenRepo.SaveAsync(MagicLinkToken.Create(member.Id, tokenHash, remoteIp));

        await email.SendRegistrationConfirmationWithMagicLinkAsync(member, magicLink);
    }
}
