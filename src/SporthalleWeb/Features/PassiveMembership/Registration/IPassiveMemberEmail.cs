using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public interface IPassiveMemberEmail
{
    Task SendRegistrationConfirmationAsync(PassiveMember member);
}
