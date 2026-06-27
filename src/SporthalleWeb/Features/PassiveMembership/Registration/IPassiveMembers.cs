using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public interface IPassiveMembers
{
    Task<bool> IsFieldTakenAsync(FieldNumber field);
    Task<PassiveMember> SaveAsync(PassiveMember member);
    Task<IReadOnlyList<PassiveMember>> GetPendingAsync();
    Task<IReadOnlyList<PassiveMember>> GetConfirmedAsync();
    Task<PassiveMember?> FindByIdAsync(int id);
    Task UpdateAsync(PassiveMember member);
    Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync();
}
