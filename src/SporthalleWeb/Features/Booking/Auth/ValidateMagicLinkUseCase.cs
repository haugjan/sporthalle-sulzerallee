using System.Security.Cryptography;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Features.Booking.Auth;

public sealed class ValidateMagicLink(
    IMagicLinkTokens tokenRepo,
    IHallMembers members)
{
    public async Task<HallMember> ExecuteAsync(string plainToken)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plainToken)));

        var token = await tokenRepo.FindByHashAsync(hash)
            ?? throw new DomainException("Ungültiger Anmelde-Link.");

        if (token.UsedAt.HasValue)
            throw new DomainException("Dieser Link wurde bereits verwendet.");
        if (token.ExpiresAt < DateTime.UtcNow)
            throw new DomainException("Der Link ist abgelaufen (Gültigkeitsdauer: 20 Minuten).");

        await tokenRepo.MarkUsedAsync(token.Id);

        var member = await members.FindByIdAsync(token.MemberId)
            ?? throw new DomainException("Mieter nicht gefunden.");

        await members.SignInAsync(member.Id);
        return member;
    }
}
