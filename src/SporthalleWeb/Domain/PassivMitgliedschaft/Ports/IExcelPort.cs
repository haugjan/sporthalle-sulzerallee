namespace SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

public interface IExcelPort
{
    byte[] ExportMembers(IReadOnlyList<PassivMitglied> members);
}
