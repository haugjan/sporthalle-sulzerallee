using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.MemberAdmin;

public interface IPassiveMemberAbaninja
{
    byte[] ExportMembers(IReadOnlyList<PassiveMember> members);
}
