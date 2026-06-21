namespace SporthalleWeb.Domain.Booking.Ports;

public interface IBookingCsvPort
{
    Task<byte[]> ExportAsync(DateTime fromUtc, DateTime toUtc);
}
