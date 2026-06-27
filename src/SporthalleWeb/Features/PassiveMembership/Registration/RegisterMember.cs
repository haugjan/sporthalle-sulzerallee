using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public record RegisterMemberCommand(
    int FieldNumber,
    string FirstName,
    string LastName,
    string AddressLine,
    string? AddressLine2,
    string PostalCode,
    string City,
    string? Phone,
    string Email,
    string LevelKey,
    bool ShowNameOnFloor,
    string? DisplayName,
    bool Consent);

public sealed class RegisterMember(
    IPassiveMembers repo,
    IPassiveMemberEmail email,
    ILogger<RegisterMember> logger)
{
    public async Task<PassiveMember> ExecuteAsync(RegisterMemberCommand cmd)
    {
        var fieldNumber = new FieldNumber(cmd.FieldNumber);

        if (await repo.IsFieldTakenAsync(fieldNumber))
            throw new FieldAlreadyTakenException(fieldNumber);

        var member = PassiveMember.Register(
            fieldNumber,
            cmd.FirstName,
            cmd.LastName,
            cmd.AddressLine,
            cmd.AddressLine2,
            cmd.PostalCode,
            cmd.City,
            cmd.Phone,
            new MemberEmail(cmd.Email),
            MembershipLevel.FromKey(cmd.LevelKey),
            cmd.ShowNameOnFloor,
            cmd.DisplayName);

        await repo.SaveAsync(member);

        try
        {
            await email.SendRegistrationConfirmationAsync(member);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Confirmation email failed for member {Email} (field {Field}); registration is saved.",
                member.Email.Value, member.FieldNumber.Value);
        }

        return member;
    }
}
