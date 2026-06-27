
using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public interface ICaptcha
{
    Task<bool> VerifyAsync(string? token, string? remoteIp);
}
