using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

namespace SporthalleWeb.Application.PassivMitgliedschaft;

public sealed class RegisterMemberUseCase
{
    private readonly IPassivMitgliederRepository _repo;
    private readonly IEmailPort _email;

    public RegisterMemberUseCase(IPassivMitgliederRepository repo, IEmailPort email)
    {
        _repo = repo;
        _email = email;
    }

    public async Task<PassivMitglied> ExecuteAsync(RegisterMemberCommand cmd)
    {
        var fieldNumber = new FieldNumber(cmd.FieldNumber);

        if (await _repo.IsFieldTakenAsync(fieldNumber))
            throw new FieldAlreadyTakenException(fieldNumber);

        var member = PassivMitglied.Register(
            fieldNumber,
            cmd.FirstName,
            cmd.LastName,
            cmd.AddressLine,
            cmd.PostalCode,
            cmd.City,
            new MemberEmail(cmd.Email),
            MembershipLevel.FromKey(cmd.LevelKey),
            cmd.ShowNameOnFloor,
            cmd.DisplayName);

        await _repo.SaveAsync(member);
        await _email.SendRegistrationConfirmationAsync(member);

        return member;
    }
}
