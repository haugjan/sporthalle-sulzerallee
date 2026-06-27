using SporthalleWeb.Domain.Booking.HallMemberAggregate;

namespace SporthalleWeb.Features.Booking.Ports;

public interface IMagicLinkTokens
{
    Task SaveAsync(MagicLinkToken token);
    Task<MagicLinkToken?> FindByHashAsync(string tokenHash);
    Task MarkUsedAsync(int tokenId);
    Task PurgeExpiredAsync();
}
