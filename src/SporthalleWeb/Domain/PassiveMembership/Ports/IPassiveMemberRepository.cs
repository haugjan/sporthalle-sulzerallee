namespace SporthalleWeb.Domain.PassiveMembership.Ports;

public interface IPassiveMemberRepository
{
    Task<bool> IsFieldTakenAsync(FieldNumber field);
    Task<PassiveMember> SaveAsync(PassiveMember member);
    Task<IReadOnlyList<PassiveMember>> GetAllAsync();
    Task<PassiveMember?> FindByIdAsync(int id);
    Task UpdateAsync(PassiveMember member);
    Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync();
}
