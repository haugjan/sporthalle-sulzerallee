using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SporthalleWeb.Features.PassiveMembership.MemberAdmin;
using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;
using SporthalleWeb.Features.PassiveMembership.Registration;
using Xunit;

namespace SporthalleWeb.Tests.Application.PassiveMembership;

public sealed class RegisterMemberUseCaseTests
{
    private readonly Mock<IPassiveMembers> _repo = new();
    private readonly Mock<IPassiveMemberEmail> _email = new();
    private readonly RegisterMember _sut;

    public RegisterMemberUseCaseTests()
    {
        _sut = new RegisterMember(
            _repo.Object, _email.Object,
            NullLogger<RegisterMember>.Instance);
    }

    private static RegisterMemberCommand ValidCommand(int field = 1) => new(
        FieldNumber: field,
        FirstName: "Max", LastName: "Muster",
        AddressLine: "Musterstrasse 1", AddressLine2: null,
        PostalCode: "8400", City: "Winterthur",
        Phone: null,
        Email: "max@muster.ch",
        LevelKey: "Bronze",
        ShowNameOnFloor: false, DisplayName: null,
        Consent: true);

    [Fact]
    public async Task Execute_ValidCommand_SavesMemberAndReturnsIt()
    {
        _repo.Setup(r => r.IsFieldTakenAsync(It.IsAny<FieldNumber>())).ReturnsAsync(false);

        var result = await _sut.ExecuteAsync(ValidCommand());

        Assert.Equal("Max", result.FirstName);
        Assert.Equal(MemberStatus.Pending, result.Status);
        _repo.Verify(r => r.SaveAsync(It.IsAny<PassiveMember>()), Times.Once);
    }

    [Fact]
    public async Task Execute_FieldTaken_ThrowsFieldAlreadyTakenException()
    {
        _repo.Setup(r => r.IsFieldTakenAsync(It.IsAny<FieldNumber>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<FieldAlreadyTakenException>(
            () => _sut.ExecuteAsync(ValidCommand()));

        _repo.Verify(r => r.SaveAsync(It.IsAny<PassiveMember>()), Times.Never);
    }

    [Fact]
    public async Task Execute_EmailFails_StillReturnsMember()
    {
        _repo.Setup(r => r.IsFieldTakenAsync(It.IsAny<FieldNumber>())).ReturnsAsync(false);
        _email.Setup(e => e.SendRegistrationConfirmationAsync(It.IsAny<PassiveMember>()))
              .ThrowsAsync(new Exception("SMTP error"));

        var result = await _sut.ExecuteAsync(ValidCommand());

        Assert.NotNull(result);
        _repo.Verify(r => r.SaveAsync(It.IsAny<PassiveMember>()), Times.Once);
    }

    [Fact]
    public async Task Execute_SendsConfirmationEmail()
    {
        _repo.Setup(r => r.IsFieldTakenAsync(It.IsAny<FieldNumber>())).ReturnsAsync(false);

        await _sut.ExecuteAsync(ValidCommand());

        _email.Verify(e => e.SendRegistrationConfirmationAsync(It.IsAny<PassiveMember>()), Times.Once);
    }

    [Fact]
    public async Task Execute_InvalidFieldNumber_ThrowsDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync(ValidCommand(field: 0)));
    }

    [Fact]
    public async Task Execute_InvalidLevelKey_ThrowsDomainException()
    {
        _repo.Setup(r => r.IsFieldTakenAsync(It.IsAny<FieldNumber>())).ReturnsAsync(false);
        var cmd = ValidCommand() with { LevelKey = "Platin" };

        await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync(cmd));
    }

    [Fact]
    public async Task Execute_InvalidEmail_ThrowsDomainException()
    {
        _repo.Setup(r => r.IsFieldTakenAsync(It.IsAny<FieldNumber>())).ReturnsAsync(false);
        var cmd = ValidCommand() with { Email = "not-an-email" };

        await Assert.ThrowsAsync<DomainException>(() => _sut.ExecuteAsync(cmd));
    }
}
