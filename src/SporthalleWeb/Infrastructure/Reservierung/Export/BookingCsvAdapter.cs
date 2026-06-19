using System.Text;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Infrastructure.Reservierung.Export;

public sealed class BookingCsvAdapter(
    IBookingSlotRepository slotRepo,
    IMemberManagerPort members) : IBookingCsvPort
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<byte[]> ExportAsync(DateTime fromUtc, DateTime toUtc, bool confirmedOnly)
    {
        var slots = await slotRepo.GetForExportAsync(fromUtc, toUtc, confirmedOnly);

        var sb = new StringBuilder();
        sb.AppendLine("Datum;Wochentag;Start;Ende;Dauer (h);Status;Anlass;Mieter;E-Mail;Preis/Block;Blöcke;Gesamtpreis;Notiz");

        foreach (var slot in slots)
        {
            HallMember? member = null;
            if (slot.MemberId.HasValue)
                member = await members.FindByIdAsync(slot.MemberId.Value);

            var start = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.StartUtc, Zurich);
            var end = TimeZoneInfo.ConvertTimeFromUtc(slot.Slot.EndUtc, Zurich);
            var durationH = (slot.Slot.EndUtc - slot.Slot.StartUtc).TotalHours;

            sb.AppendLine(string.Join(";",
                start.ToString("yyyy-MM-dd"),
                start.ToString("dddd"),
                start.ToString("HH:mm"),
                end.ToString("HH:mm"),
                durationH.ToString("0.##"),
                slot.Status.ToString(),
                CsvEscape(slot.EventType),
                CsvEscape(member?.ContactPerson),
                CsvEscape(member?.Email),
                slot.PricePerBlock?.ToString("0.00") ?? "",
                slot.TotalBlocks?.ToString() ?? "",
                slot.TotalPrice?.ToString("0.00") ?? "",
                CsvEscape(slot.PriceNote)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
