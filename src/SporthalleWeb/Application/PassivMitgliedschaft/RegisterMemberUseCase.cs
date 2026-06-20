using Microsoft.Extensions.Logging;
using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

namespace SporthalleWeb.Application.PassivMitgliedschaft;

public sealed class RegisterMemberUseCase
{
    private readonly IPassivMitgliederRepository _repo;
    private readonly IEmailPort _email;
    private readonly ILogger<RegisterMemberUseCase> _logger;

    public RegisterMemberUseCase(
        IPassivMitgliederRepository repo,
        IEmailPort email,
        ILogger<RegisterMemberUseCase> logger)
    {
        _repo = repo;
        _email = email;
        _logger = logger;
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

        try
        {
            await _email.SendRegistrationConfirmationAsync(member);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Confirmation email failed for member {Email} (field {Field}); registration is saved.",
                member.Email.Value, member.FieldNumber.Value);
        }

        return member;
    }
}
