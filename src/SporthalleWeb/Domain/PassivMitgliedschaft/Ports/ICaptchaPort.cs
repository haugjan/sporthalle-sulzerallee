namespace SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

public interface ICaptchaPort
{
    Task<bool> VerifyAsync(string token, string remoteIp);
}
