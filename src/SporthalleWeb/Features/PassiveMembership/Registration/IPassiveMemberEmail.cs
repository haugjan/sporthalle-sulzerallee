using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public interface IPassiveMemberEmail
{
    Task SendRegistrationConfirmationAsync(PassiveMember member);
}
