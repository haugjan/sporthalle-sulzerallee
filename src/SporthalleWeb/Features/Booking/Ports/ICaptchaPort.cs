
namespace SporthalleWeb.Features.Booking.Ports;

public interface ICaptcha
{
    Task<bool> VerifyAsync(string? token, string? remoteIp);
}
