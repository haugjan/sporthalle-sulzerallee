namespace SporthalleWeb.Domain.PassiveMembership.Ports;

public interface IEmailPort
{
    Task SendRegistrationConfirmationAsync(PassiveMember member);
}
