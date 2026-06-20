using SporthalleWeb.Domain.PassivMitgliedschaft;
using SporthalleWeb.Domain.PassivMitgliedschaft.Ports;

namespace SporthalleWeb.Application.PassivMitgliedschaft;

public sealed class GetFieldStatusesQuery
{
    private readonly IPassivMitgliederRepository _repo;

    public GetFieldStatusesQuery(IPassivMitgliederRepository repo) => _repo = repo;

    public async Task<FieldStatusesResult> ExecuteAsync()
    {
        var occupied = await _repo.GetOccupiedFieldsAsync();
        var fields = occupied
            .Select(f => new FieldStatusDto(f.Field.Value, f.DisplayName, VipField.GetLabel(f.Field.Value)))
            .ToList();
        return new FieldStatusesResult(fields, TotalFields: 300);
    }
}
