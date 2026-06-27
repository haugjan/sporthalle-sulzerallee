using SporthalleWeb.Domain.Booking;

namespace SporthalleWeb.Features.Booking;

public interface IBookingCsv
{
    Task<byte[]> ExportAsync(DateTime fromUtc, DateTime toUtc);
}
