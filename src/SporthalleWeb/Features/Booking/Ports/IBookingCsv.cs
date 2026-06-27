namespace SporthalleWeb.Features.Booking.Ports;

public interface IBookingCsv
{
    Task<byte[]> ExportAsync(DateTime fromUtc, DateTime toUtc);
}
