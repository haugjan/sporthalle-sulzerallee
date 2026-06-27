using Moq;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;
using Xunit;


using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Tests.Application.Booking;

public sealed class RegisterRenterUseCaseTests
{
    private readonly Mock<IHallMembers> _members = new();
    private readonly Mock<IMagicLinkTokens> _tokens = new();
    private readonly Mock<ICaptcha> _captcha = new();
    private readonly Mock<IBookingEmail> _email = new();
    private readonly RegisterRenter _sut;

    public RegisterRenterUseCaseTests()
    {
        _captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(true);
        _sut = new RegisterRenter(_members.Object, _tokens.Object, _captcha.Object, _email.Object);
    }

    // ── Name validation ───────────────────────────────────────────────────────
    // Umbraco rejects members with an empty Name ("All variants must have a name").
    // UmbracoHallMembers derives Name from ContactFirstName + ContactLastName.

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("", "   ")]
    [InlineData("   ", "")]
    public async Task ExecuteAsync_WhenBothNamesEmptyOrWhitespace_ThrowsDomainException(string first, string last)
    {
        var cmd = MakeCommand(firstName: first, lastName: last);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.ExecuteAsync(cmd, "token", "1.2.3.4"));

        Assert.Contains("Nachname", ex.Message);
    }

    [Theory]
    [InlineData("Sandra", "")]
    [InlineData("", "Zollinger")]
    [InlineData("Sandra", "Zollinger")]
    public async Task ExecuteAsync_WhenAtLeastOneNameProvided_DoesNotThrowNameError(string first, string last)
    {
        SetupHappyPath();
        var cmd = MakeCommand(firstName: first, lastName: last);

        // Should not throw a "name required" exception; any other DomainException
        // (e.g. duplicate email) is unrelated to this specific guard.
        var exception = await Record.ExceptionAsync(() => _sut.ExecuteAsync(cmd, "token", "1.2.3.4"));

        Assert.True(exception is null || (exception is DomainException de && !de.Message.Contains("Nachname")),
            $"Unexpected exception: {exception?.Message}");
    }

    // ── CAPTCHA ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenCaptchaFails_ThrowsDomainException()
    {
        _captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync(false);

        var cmd = MakeCommand();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.ExecuteAsync(cmd, "bad-token", "1.2.3.4"));

        Assert.Contains("CAPTCHA", ex.Message);
    }

    // ── Duplicate email ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenEmailAlreadyRegistered_ThrowsDomainException()
    {
        var existing = StubMember();
        _members.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(existing);

        var cmd = MakeCommand();

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _sut.ExecuteAsync(cmd, "token", "1.2.3.4"));

        Assert.Contains("registriert", ex.Message);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WithValidCommand_CreatesAndEmailsMember()
    {
        SetupHappyPath();
        var cmd = MakeCommand();

        await _sut.ExecuteAsync(cmd, "token", "1.2.3.4");

        _members.Verify(m => m.CreateAsync(cmd, cmd.Password), Times.Once);
        _email.Verify(e => e.SendRegistrationConfirmationWithMagicLinkAsync(
            It.IsAny<HallMember>(), It.Is<string>(s => s.Contains("/reservierung/auth/validate"))), Times.Once);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupHappyPath()
    {
        _members.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((HallMember?)null);
        _members.Setup(m => m.CreateAsync(It.IsAny<RegisterRenterCommand>(), It.IsAny<string?>()))
                .ReturnsAsync(StubMember());
        _tokens.Setup(t => t.SaveAsync(It.IsAny<MagicLinkToken>())).Returns(Task.CompletedTask);
        _email.Setup(e => e.SendRegistrationConfirmationWithMagicLinkAsync(
                    It.IsAny<HallMember>(), It.IsAny<string>()))
              .Returns(Task.CompletedTask);
    }

    private static HallMember StubMember() => new(
        Id: 1,
        Email: "test@example.com",
        RenterType: new RenterType("Privatperson"),
        Name: null,
        ContactFirstName: "Sandra",
        ContactLastName: "Zollinger",
        BillingAddress: "Testgasse 1",
        AddressLine2: null,
        BillingPostalCode: "8400",
        BillingCity: "Winterthur",
        BillingCountry: "Schweiz",
        Phone: null,
        Notes: null,
        HasKey: false,
        HasPassword: false
    );

    private static RegisterRenterCommand MakeCommand(
        string firstName = "Sandra",
        string lastName  = "Zollinger") =>
        new(
            Email:              "test@example.com",
            RenterType:         new RenterType("Privatperson"),
            Name:               null,
            ContactFirstName:   firstName,
            ContactLastName:    lastName,
            BillingAddress:     "Testgasse 1",
            AddressLine2:       null,
            BillingPostalCode:  "8400",
            BillingCity:        "Winterthur",
            BillingCountry:     "Schweiz",
            Phone:              null,
            HasKey:             false,
            Password:           null
        );
}
