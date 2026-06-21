using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;

namespace SporthalleWeb.Application.PassiveMembership;

public sealed class GetFieldStatusesQuery
{
    private readonly IPassiveMemberRepository _repo;

    public GetFieldStatusesQuery(IPassiveMemberRepository repo) => _repo = repo;

    public async Task<FieldStatusesResult> ExecuteAsync()
    {
        var occupied = await _repo.GetOccupiedFieldsAsync();
        var fields = occupied
            .Select(f => new FieldStatusDto(f.Field.Value, f.DisplayName, VipField.GetLabel(f.Field.Value)))
            .ToList();
        return new FieldStatusesResult(fields, TotalFields: 300);
    }
}
