using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

public interface IPassiveMemberExport
{
    byte[] ExportMembers(IReadOnlyList<PassiveMember> members);
}
