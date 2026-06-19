namespace SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

public interface IPassivMitgliederRepository
{
    Task<bool> IsFieldTakenAsync(FieldNumber field);
    Task<PassivMitglied> SaveAsync(PassivMitglied member);
    Task<IReadOnlyList<PassivMitglied>> GetAllAsync();
    Task<PassivMitglied?> FindByIdAsync(int id);
    Task UpdateAsync(PassivMitglied member);
    Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync();
}
