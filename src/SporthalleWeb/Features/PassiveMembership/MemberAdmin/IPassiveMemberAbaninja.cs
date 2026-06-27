using SporthalleWeb.Features.PassiveMembership.Registration.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

public interface IPassiveMemberAbaninja
{
    byte[] ExportMembers(IReadOnlyList<PassiveMember> members);
}
