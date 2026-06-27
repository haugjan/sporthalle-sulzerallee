using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;

namespace SporthalleWeb.Tests.Domain.Booking;

public sealed class MagicLinkTokenTests
{
    [Fact]
    public void Create_SetsPropertiesAndFutureExpiry()
    {
        var before = DateTime.UtcNow;
        var token = MagicLinkToken.Create(1, "hash123", "127.0.0.1");
        var after = DateTime.UtcNow;

        Assert.Equal(1, token.MemberId);
        Assert.Equal("hash123", token.TokenHash);
        Assert.Equal("127.0.0.1", token.RemoteIp);
        Assert.Null(token.UsedAt);
        Assert.True(token.ExpiresAt > before.AddMinutes(19));
        Assert.True(token.ExpiresAt < after.AddMinutes(21));
    }

    [Fact]
    public void IsValid_UnusedAndNotExpired_ReturnsTrue()
    {
        var token = MagicLinkToken.FromPersistence(
            1, 1, "h", DateTime.UtcNow.AddMinutes(10), null, DateTime.UtcNow, null);
        Assert.True(token.IsValid());
    }

    [Fact]
    public void IsValid_AlreadyUsed_ReturnsFalse()
    {
        var token = MagicLinkToken.FromPersistence(
            1, 1, "h", DateTime.UtcNow.AddMinutes(10),
            usedAt: DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow, null);
        Assert.False(token.IsValid());
    }

    [Fact]
    public void IsValid_Expired_ReturnsFalse()
    {
        var token = MagicLinkToken.FromPersistence(
            1, 1, "h",
            expiresAt: DateTime.UtcNow.AddMinutes(-1),
            null, DateTime.UtcNow, null);
        Assert.False(token.IsValid());
    }

    [Fact]
    public void MarkUsed_SetsUsedAt()
    {
        var token = MagicLinkToken.Create(1, "h", null);
        Assert.Null(token.UsedAt);
        token.MarkUsed();
        Assert.NotNull(token.UsedAt);
    }

    [Fact]
    public void FromPersistence_RestoresAllFields()
    {
        var expires = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = MagicLinkToken.FromPersistence(42, 7, "hash", expires, null, created, "::1");

        Assert.Equal(42, token.Id);
        Assert.Equal(7, token.MemberId);
        Assert.Equal("hash", token.TokenHash);
        Assert.Equal(DateTimeKind.Utc, token.ExpiresAt.Kind);
        Assert.Equal(DateTimeKind.Utc, token.CreatedAt.Kind);
    }
}
