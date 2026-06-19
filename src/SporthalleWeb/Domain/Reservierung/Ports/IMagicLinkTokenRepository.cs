namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IMagicLinkTokenRepository
{
    Task SaveAsync(MagicLinkToken token);
    Task<MagicLinkToken?> FindByHashAsync(string tokenHash);
    Task MarkUsedAsync(int tokenId);
    Task PurgeExpiredAsync();
}
