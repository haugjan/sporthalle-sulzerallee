using System.Text;
using SporthalleWeb.Features.Booking;
using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Infrastructure.Booking;

public sealed class BookingCsvExport(
    IBookingSlots slotRepo,
    IHallMembers members) : IBookingCsv
{
    private static readonly TimeZoneInfo Zurich =
        TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

    public async Task<byte[]> ExportAsync(DateTime fromUtc, DateTime toUtc)
    {
        var from = DateOnly.FromDateTime(fromUtc);
        var to = DateOnly.FromDateTime(toUtc);
        var slots = await slotRepo.GetAllAsync(from, to, null);
        var exportable = slots.Where(s => s.Type != SlotType.Blocker).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Datum;Wochentag;Start;Ende;Dauer (h);Typ;Bezeichnung;Mieter;E-Mail;Notiz");

        foreach (var slot in exportable)
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
                slot.Type.ToString(),
                CsvEscape(slot.Title),
                CsvEscape(member is null ? null : $"{member.ContactFirstName} {member.ContactLastName}".Trim()),
                CsvEscape(member?.Email),
                CsvEscape(slot.Notes)));
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
