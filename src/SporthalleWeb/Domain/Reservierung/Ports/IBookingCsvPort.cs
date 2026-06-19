namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IBookingCsvPort
{
    Task<byte[]> ExportAsync(DateTime fromUtc, DateTime toUtc, bool confirmedOnly);
}
