namespace SporthalleWeb.Features.Booking;

public interface IMagicLinkTokens
{
    Task SaveAsync(MagicLinkToken token);
    Task<MagicLinkToken?> FindByHashAsync(string tokenHash);
    Task MarkUsedAsync(int tokenId);
    Task PurgeExpiredAsync();
}
