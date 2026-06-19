namespace SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

public interface IEmailPort
{
    Task SendRegistrationConfirmationAsync(PassivMitglied member);
}
