namespace SporthalleWeb.Domain.PassiveMembership.Ports;

public interface ICaptchaPort
{
    Task<bool> VerifyAsync(string token, string remoteIp);
}
