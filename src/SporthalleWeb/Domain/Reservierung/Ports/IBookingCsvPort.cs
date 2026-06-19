namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IBookingCsvPort
{
    byte[] ExportBookings(
        IReadOnlyList<(BookingSlot Slot, HallMember? Member)> data,
        DateTime from, DateTime to);
}
