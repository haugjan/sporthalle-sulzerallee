namespace SporthalleWeb.Domain.PassiveMembership.Ports;

public interface IAbaninjaCsvPort
{
    byte[] ExportMembers(IReadOnlyList<PassiveMember> members);
}
