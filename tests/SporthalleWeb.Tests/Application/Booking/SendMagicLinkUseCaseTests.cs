using Moq;
using SporthalleWeb.Application.Booking;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;
using Xunit;

namespace SporthalleWeb.Tests.Application.Booking;

public sealed class SendMagicLinkUseCaseTests
{
    private readonly Mock<IMemberManagerPort> _members = new();
    private readonly Mock<IMagicLinkTokenRepository> _tokenRepo = new();
    private readonly Mock<IBookingEmailPort> _email = new();
    private readonly SendMagicLinkUseCase _sut;

    public SendMagicLinkUseCaseTests()
    {
        _sut = new SendMagicLinkUseCase(_members.Object, _tokenRepo.Object, _email.Object);
    }

    private static HallMember MakeHallMember(int id = 1) => new(
        id, "user@example.com", new RenterType("Privatperson"),
        "Test", "Max", "Muster", "Str 1", null,
        "8400", "Winterthur", "Schweiz",
        null, null, false, false, null, null);

    [Fact]
    public async Task Execute_UnknownEmail_ReturnsFalse()
    {
        _members.Setup(m => m.FindByEmailAsync("unknown@example.com")).ReturnsAsync((HallMember?)null);

        var result = await _sut.ExecuteAsync("unknown@example.com", null);

        Assert.False(result);
        _email.Verify(e => e.SendMagicLinkAsync(It.IsAny<HallMember>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_KnownEmail_SavesTokenAndSendsEmail()
    {
        var member = MakeHallMember();
        _members.Setup(m => m.FindByEmailAsync("user@example.com")).ReturnsAsync(member);
        _members.Setup(m => m.GetMagicLinkSentAtAsync(1)).ReturnsAsync((DateTime?)null);

        var result = await _sut.ExecuteAsync("User@Example.COM", null);

        Assert.True(result);
        _tokenRepo.Verify(r => r.SaveAsync(It.IsAny<MagicLinkToken>()), Times.Once);
        _email.Verify(e => e.SendMagicLinkAsync(member, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WithinRateLimit_ThrowsDomainException()
    {
        var member = MakeHallMember();
        _members.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(member);
        _members.Setup(m => m.GetMagicLinkSentAtAsync(1))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(-5)); // within 10-minute window

        await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync("user@example.com", null));

        _email.Verify(e => e.SendMagicLinkAsync(It.IsAny<HallMember>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Execute_AfterRateLimit_Succeeds()
    {
        var member = MakeHallMember();
        _members.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(member);
        _members.Setup(m => m.GetMagicLinkSentAtAsync(1))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(-11)); // outside window

        var result = await _sut.ExecuteAsync("user@example.com", null);

        Assert.True(result);
    }

    [Fact]
    public void GenerateToken_ProducesUniqueTokenPairs()
    {
        var (plain1, hash1) = SendMagicLinkUseCase.GenerateToken();
        var (plain2, hash2) = SendMagicLinkUseCase.GenerateToken();

        Assert.NotEqual(plain1, plain2);
        Assert.NotEqual(hash1, hash2);
        Assert.NotEmpty(plain1);
        Assert.NotEmpty(hash1);
    }

    [Fact]
    public void GenerateToken_HashIsHexString()
    {
        var (_, hash) = SendMagicLinkUseCase.GenerateToken();
        Assert.Matches("^[0-9A-F]+$", hash);
    }
}
