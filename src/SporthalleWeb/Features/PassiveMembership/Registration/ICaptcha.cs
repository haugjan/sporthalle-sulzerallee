namespace SporthalleWeb.Features.PassiveMembership.Registration;

public interface ICaptcha
{
    Task<bool> VerifyAsync(string token, string remoteIp);
}
