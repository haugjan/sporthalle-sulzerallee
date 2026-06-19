namespace SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

public interface IAbaninjaCsvPort
{
    byte[] ExportMembers(IReadOnlyList<PassivMitglied> members);
}
