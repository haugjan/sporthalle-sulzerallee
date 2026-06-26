using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;

namespace SporthalleWeb.Application.Booking;

public sealed class SendMagicLinkUseCase(
    IMemberManagerPort members,
    IMagicLinkTokenRepository tokenRepo,
    IBookingEmailPort email)
{
    public async Task<bool> ExecuteAsync(string emailRaw, string? remoteIp)
    {
        var member = await members.FindByEmailAsync(emailRaw.Trim().ToLowerInvariant());
        if (member is null) return false;

        var (plainToken, tokenHash) = GenerateToken();
        var magicLink = $"https://www.sporthalle-sulzerallee.ch/reservierung/auth/validate?token={plainToken}";

        await tokenRepo.SaveAsync(MagicLinkToken.Create(member.Id, tokenHash, remoteIp));
        await email.SendMagicLinkAsync(member, magicLink);

        return true;
    }

    internal static (string plain, string hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = Base64UrlTextEncoder.Encode(bytes);
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plain)));
        return (plain, hash);
    }
}
