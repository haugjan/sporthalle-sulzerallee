namespace SporthalleWeb.Domain.Booking;

public sealed class MagicLinkToken
{
    public int Id { get; private set; }
    public int MemberId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public DateTime ExpiresAt { get; private init; }
    public DateTime? UsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? RemoteIp { get; private set; }

    private MagicLinkToken() { }

    public static MagicLinkToken Create(int memberId, string tokenHash, string? remoteIp) =>
        new()
        {
            MemberId = memberId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(20),
            CreatedAt = DateTime.UtcNow,
            RemoteIp = remoteIp
        };

    public static MagicLinkToken FromPersistence(
        int id, int memberId, string tokenHash,
        DateTime expiresAt, DateTime? usedAt, DateTime createdAt, string? remoteIp) =>
        new()
        {
            Id = id,
            MemberId = memberId,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc),
            UsedAt = usedAt.HasValue ? DateTime.SpecifyKind(usedAt.Value, DateTimeKind.Utc) : null,
            CreatedAt = DateTime.SpecifyKind(createdAt, DateTimeKind.Utc),
            RemoteIp = remoteIp
        };

    public bool IsValid() => UsedAt is null && DateTime.UtcNow < ExpiresAt;

    public void MarkUsed() => UsedAt = DateTime.UtcNow;
}
