using NPoco;
using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;
using Umbraco.Cms.Infrastructure.Scoping;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Persistence;

public class PassiveMemberRepository : IPassiveMemberRepository
{
    private readonly IScopeProvider _scopeProvider;

    public PassiveMemberRepository(IScopeProvider scopeProvider)
        => _scopeProvider = scopeProvider;

    public async Task<bool> IsFieldTakenAsync(FieldNumber field)
    {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var count = await scope.Database.ExecuteScalarAsync<int>(
            new Sql("SELECT COUNT(*) FROM PassivMitglieder WHERE FieldNumber = @0", field.Value));
        return count > 0;
    }

    public async Task<PassiveMember> SaveAsync(PassiveMember member)
    {
        using var scope = _scopeProvider.CreateScope();
        var record = ToRecord(member);
        await scope.Database.InsertAsync(record);
        scope.Complete();
        return PassiveMember.Reconstitute(
            record.Id, record.FieldNumber, record.FirstName, record.LastName,
            record.AddressLine, record.PostalCode, record.City, record.Country,
            record.Email, record.MembershipLevel,
            record.ShowNameOnFloor, record.DisplayName,
            record.CreatedAt, record.PaidAt, record.Notes);
    }

    public async Task<IReadOnlyList<PassiveMember>> GetAllAsync()
    {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var records = await scope.Database.FetchAsync<PassiveMemberDbRecord>(
            "SELECT * FROM PassivMitglieder ORDER BY Id");
        return records.Select(ToEntity).ToList();
    }

    public async Task<PassiveMember?> FindByIdAsync(int id)
    {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var record = await scope.Database.SingleOrDefaultAsync<PassiveMemberDbRecord>(
            new Sql("WHERE Id = @0", id));
        return record is null ? null : ToEntity(record);
    }

    public async Task UpdateAsync(PassiveMember member)
    {
        using var scope = _scopeProvider.CreateScope();
        await scope.Database.UpdateAsync(ToRecord(member));
        scope.Complete();
    }

    public async Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync()
    {
        using var scope = _scopeProvider.CreateScope(autoComplete: true);
        var records = await scope.Database.FetchAsync<PassiveMemberDbRecord>(
            "SELECT FieldNumber, DisplayName FROM PassivMitglieder");
        return records
            .Select(r => (new FieldNumber(r.FieldNumber), r.DisplayName))
            .ToList();
    }

    private static PassiveMemberDbRecord ToRecord(PassiveMember m) => new()
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

    private static PassiveMember ToEntity(PassiveMemberDbRecord r) =>
        PassiveMember.Reconstitute(
            r.Id, r.FieldNumber, r.FirstName, r.LastName,
            r.AddressLine, r.PostalCode, r.City, r.Country,
            r.Email, r.MembershipLevel,
            r.ShowNameOnFloor, r.DisplayName,
            r.CreatedAt, r.PaidAt, r.Notes);
}
