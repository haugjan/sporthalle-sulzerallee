using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class SendMagicLinkUseCase(
    IMemberManagerPort members,
    IMagicLinkTokenRepository tokenRepo,
    IBookingEmailPort email)
{
    public async Task<bool> ExecuteAsync(string emailRaw, string? remoteIp)
    {
        var member = await members.FindByEmailAsync(emailRaw.Trim().ToLowerInvariant());
        if (member is null) return false;

        var lastSent = await members.GetMagicLinkSentAtAsync(member.Id);
        if (lastSent.HasValue && (DateTime.UtcNow - lastSent.Value).TotalMinutes < 10)
            throw new DomainException("Bitte warte 10 Minuten bevor du einen neuen Link anforderst.");

        var (plainToken, tokenHash) = GenerateToken();
        var magicLink = $"https://www.sporthalle-sulzerallee.ch/reservierung/auth/validate?token={plainToken}";

        await tokenRepo.SaveAsync(MagicLinkToken.Create(member.Id, tokenHash, remoteIp));
        await members.SetMagicLinkSentAtAsync(member.Id, DateTime.UtcNow);
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
