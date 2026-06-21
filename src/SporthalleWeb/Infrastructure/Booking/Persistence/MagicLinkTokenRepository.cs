using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;
using SporthalleWeb.Infrastructure.Booking.Persistence.DbRecords;

namespace SporthalleWeb.Infrastructure.Booking.Persistence;

public sealed class MagicLinkTokenRepository(IScopeProvider scopeProvider) : IMagicLinkTokenRepository
{
    public async Task SaveAsync(MagicLinkToken token)
    {
        using var scope = scopeProvider.CreateScope();
        var record = new MagicLinkTokenRecord
        {
            MemberId = token.MemberId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            UsedAt = token.UsedAt,
            CreatedAt = token.CreatedAt,
            RemoteIp = token.RemoteIp
        };
        await scope.Database.InsertAsync(record);
        scope.Complete();
    }

    public async Task<MagicLinkToken?> FindByHashAsync(string tokenHash)
    {
        using var scope = scopeProvider.CreateScope();
        var record = await scope.Database.FirstOrDefaultAsync<MagicLinkTokenRecord>(
            new Sql("SELECT * FROM MagicLinkTokens WHERE TokenHash = @0", tokenHash));
        scope.Complete();
        if (record is null) return null;
        return MagicLinkToken.FromPersistence(
            record.Id, record.MemberId, record.TokenHash,
            DateTime.SpecifyKind(record.ExpiresAt, DateTimeKind.Utc),
            record.UsedAt.HasValue ? DateTime.SpecifyKind(record.UsedAt.Value, DateTimeKind.Utc) : null,
            DateTime.SpecifyKind(record.CreatedAt, DateTimeKind.Utc),
            record.RemoteIp);
    }

    public async Task MarkUsedAsync(int tokenId)
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("UPDATE MagicLinkTokens SET UsedAt = @0 WHERE Id = @1",
                DateTime.UtcNow, tokenId));
        scope.Complete();
    }

    public async Task PurgeExpiredAsync()
    {
        using var scope = scopeProvider.CreateScope();
        await scope.Database.ExecuteAsync(
            new Sql("DELETE FROM MagicLinkTokens WHERE ExpiresAt < @0 AND UsedAt IS NOT NULL",
                DateTime.UtcNow));
        scope.Complete();
    }
}
