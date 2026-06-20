namespace SporthalleWeb.Domain.Shared;

public interface ICaptchaPort
{
    Task<bool> VerifyAsync(string? token, string? remoteIp);
}
