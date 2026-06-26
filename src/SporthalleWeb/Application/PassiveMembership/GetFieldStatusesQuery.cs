using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;

namespace SporthalleWeb.Application.PassiveMembership;

public sealed class GetFieldStatusesQuery(IPassiveMemberRepository repo)
{
    public async Task<FieldStatusesResult> ExecuteAsync()
    {
        var occupied = await repo.GetOccupiedFieldsAsync();
        var fields = occupied
            .Select(f => new FieldStatusDto(f.Field.Value, f.DisplayName, VipField.GetLabel(f.Field.Value)))
            .ToList();
        return new FieldStatusesResult(fields, TotalFields: 300);
    }
}
