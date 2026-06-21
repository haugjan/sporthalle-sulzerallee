namespace SporthalleWeb.Domain.PassiveMembership.Ports;

public interface IExcelPort
{
    byte[] ExportMembers(IReadOnlyList<PassiveMember> members);
}
