using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

public interface IPassiveMemberExport
{
    byte[] ExportMembers(IReadOnlyList<PassiveMember> members);
}
