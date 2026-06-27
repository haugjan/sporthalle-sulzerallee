using Moq;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;
using SporthalleWeb.Features.Booking.Auth;
using SporthalleWeb.Features.Booking.Ports;

namespace SporthalleWeb.Tests.Application.Booking;

public sealed class ValidateMagicLinkUseCaseTests
{
    private readonly Mock<IMagicLinkTokens> _tokenRepo = new();
    private readonly Mock<IHallMembers> _members = new();
    private readonly ValidateMagicLink _sut;

    public ValidateMagicLinkUseCaseTests()
    {
        _sut = new ValidateMagicLink(_tokenRepo.Object, _members.Object);
    }

    private static HallMember MakeHallMember(int id = 1) => new(
        id, "user@example.com", new RenterType("Privatperson"),
        "Test", "Max", "Muster", "Str 1", null,
        "8400", "Winterthur", "Schweiz",
        null, null, false, false);

    private static MagicLinkToken ValidToken(int memberId = 1)
    {
        var (_, hash) = SendMagicLink.GenerateToken();
        return MagicLinkToken.FromPersistence(
            1, memberId, hash,
            expiresAt: DateTime.UtcNow.AddMinutes(15),
            usedAt: null, createdAt: DateTime.UtcNow, remoteIp: null);
    }

    [Fact]
    public async Task Execute_ValidToken_SignsInAndReturnsMember()
    {
        var token = ValidToken();
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>())).ReturnsAsync(token);
        var member = MakeHallMember();
        _members.Setup(m => m.FindByIdAsync(1)).ReturnsAsync(member);

        var result = await _sut.ExecuteAsync("any-plain-token");

        Assert.Equal(member, result);
        _members.Verify(m => m.SignInAsync(1), Times.Once);
        _tokenRepo.Verify(r => r.MarkUsedAsync(token.Id), Times.Once);
    }

    [Fact]
    public async Task Execute_UnknownHash_ThrowsDomainException()
    {
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>())).ReturnsAsync((MagicLinkToken?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync("bad-token"));
    }

    [Fact]
    public async Task Execute_AlreadyUsedToken_ThrowsDomainException()
    {
        var token = MagicLinkToken.FromPersistence(
            1, 1, "h", DateTime.UtcNow.AddMinutes(15),
            usedAt: DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow, null);
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>())).ReturnsAsync(token);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync("used-token"));
        Assert.Contains("bereits verwendet", ex.Message);
    }

    [Fact]
    public async Task Execute_ExpiredToken_ThrowsDomainException()
    {
        var token = MagicLinkToken.FromPersistence(
            1, 1, "h",
            expiresAt: DateTime.UtcNow.AddMinutes(-1),
            usedAt: null, DateTime.UtcNow, null);
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>())).ReturnsAsync(token);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync("expired-token"));
        Assert.Contains("abgelaufen", ex.Message);
    }

    [Fact]
    public async Task Execute_MemberNotFound_ThrowsDomainException()
    {
        var token = ValidToken(memberId: 99);
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>())).ReturnsAsync(token);
        _members.Setup(m => m.FindByIdAsync(99)).ReturnsAsync((HallMember?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync("ok-token"));
    }
}
