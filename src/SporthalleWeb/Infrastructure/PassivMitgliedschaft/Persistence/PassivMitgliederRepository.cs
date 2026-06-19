using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;
using Umbraco.Cms.Core.Scoping;

namespace SporthalleWeb.Infrastructure.PassivMitgliedschaft.Persistence;

public class PassivMitgliederRepository : IPassivMitgliederRepository
{
    private readonly ICoreScopeProvider _scopeProvider;

    public PassivMitgliederRepository(ICoreScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    public async Task<bool> IsFieldTakenAsync(FieldNumber field)
    {
        using var scope = _scopeProvider.CreateCoreScope(autoComplete: true);
        var count = await scope.Database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM PassivMitglieder WHERE FieldNumber = @0", field.Value);
        return count > 0;
    }

    public async Task<PassivMitglied> SaveAsync(PassivMitglied member)
    {
        using var scope = _scopeProvider.CreateCoreScope();
        var record = ToRecord(member);
        await scope.Database.InsertAsync(record);
        scope.Complete();
        return PassivMitglied.Reconstitute(
            record.Id, record.FieldNumber, record.FirstName, record.LastName,
            record.AddressLine, record.PostalCode, record.City, record.Country,
            record.Email, record.MembershipLevel,
            record.ShowNameOnFloor, record.DisplayName,
            record.CreatedAt, record.PaidAt, record.Notes);
    }

    public async Task<IReadOnlyList<PassivMitglied>> GetAllAsync()
    {
        using var scope = _scopeProvider.CreateCoreScope(autoComplete: true);
        var records = await scope.Database.FetchAsync<PassivMitgliedDbRecord>(
            "SELECT * FROM PassivMitglieder ORDER BY Id");
        return records.Select(ToEntity).ToList();
    }

    public async Task<PassivMitglied?> FindByIdAsync(int id)
    {
        using var scope = _scopeProvider.CreateCoreScope(autoComplete: true);
        var record = await scope.Database.SingleOrDefaultAsync<PassivMitgliedDbRecord>(
            "WHERE Id = @0", id);
        return record is null ? null : ToEntity(record);
    }

    public async Task UpdateAsync(PassivMitglied member)
    {
        using var scope = _scopeProvider.CreateCoreScope();
        await scope.Database.UpdateAsync(ToRecord(member));
        scope.Complete();
    }

    public async Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync()
    {
        using var scope = _scopeProvider.CreateCoreScope(autoComplete: true);
        var records = await scope.Database.FetchAsync<PassivMitgliedDbRecord>(
            "SELECT FieldNumber, DisplayName FROM PassivMitglieder");
        return records
            .Select(r => (new FieldNumber(r.FieldNumber), r.DisplayName))
            .ToList();
    }

    private static PassivMitgliedDbRecord ToRecord(PassivMitglied m) => new()
    {
        Id = m.Id,
        FieldNumber = m.FieldNumber.Value,
        FirstName = m.FirstName,
        LastName = m.LastName,
        AddressLine = m.AddressLine,
        PostalCode = m.PostalCode,
        City = m.City,
        Country = m.Country,
        Email = m.Email.Value,
        MembershipLevel = m.Level.Key,
        ShowNameOnFloor = m.ShowNameOnFloor,
        DisplayName = m.DisplayName,
        CreatedAt = m.CreatedAt,
        PaidAt = m.PaidAt,
        Notes = m.Notes
    };

    private static PassivMitglied ToEntity(PassivMitgliedDbRecord r) =>
        PassivMitglied.Reconstitute(
            r.Id, r.FieldNumber, r.FirstName, r.LastName,
            r.AddressLine, r.PostalCode, r.City, r.Country,
            r.Email, r.MembershipLevel,
            r.ShowNameOnFloor, r.DisplayName,
            r.CreatedAt, r.PaidAt, r.Notes);
}
