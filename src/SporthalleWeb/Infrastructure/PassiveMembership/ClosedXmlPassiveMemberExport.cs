using ClosedXML.Excel;
using SporthalleWeb.Features.PassiveMembership.MemberAdmin;
using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Infrastructure.PassiveMembership;

public sealed class ClosedXmlPassiveMemberExport : IPassiveMemberExport
{
    public byte[] ExportMembers(IReadOnlyList<PassiveMember> members)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Passivmitglieder");

        string[] headers =
        [
            "Nr.", "Feld-Nr.", "VIP-Zone", "Stufe", "CHF/Jahr",
            "Vorname", "Nachname", "Adresse", "Adresszusatz", "PLZ", "Stadt",
            "Telefon", "E-Mail", "Angemeldet am", "Bezahlt am", "Notizen"
        ];

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1A5FAD");
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var row = 0; row < members.Count; row++)
        {
            var m = members[row];
            var r = row + 2;
            ws.Cell(r, 1).Value = row + 1;
            ws.Cell(r, 2).Value = m.FieldNumber.Value;
            ws.Cell(r, 3).Value = VipField.GetLabel(m.FieldNumber.Value) ?? "";
            ws.Cell(r, 4).Value = m.Level.DisplayName;
            ws.Cell(r, 5).Value = (double)m.Level.YearlyFee;
            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(r, 6).Value = m.FirstName;
            ws.Cell(r, 7).Value = m.LastName;
            ws.Cell(r, 8).Value = m.AddressLine;
            ws.Cell(r, 9).Value = m.AddressLine2 ?? "";
            ws.Cell(r, 10).Value = m.PostalCode.Value;
            ws.Cell(r, 11).Value = m.City;
            ws.Cell(r, 12).Value = m.Phone ?? "";
            ws.Cell(r, 13).Value = m.Email.Value;
            ws.Cell(r, 14).Value = m.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy");
            ws.Cell(r, 15).Value = m.PaidAt.HasValue ? m.PaidAt.Value.ToLocalTime().ToString("dd.MM.yyyy") : "";
            ws.Cell(r, 16).Value = m.Notes ?? "";
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
