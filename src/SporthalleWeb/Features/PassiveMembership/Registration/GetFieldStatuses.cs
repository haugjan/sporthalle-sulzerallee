using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;

namespace SporthalleWeb.Features.PassiveMembership.Registration;

public record FieldStatusDto(int FieldNumber, string? DisplayName, string? VipLabel);

public record FieldStatusesResult(IReadOnlyList<FieldStatusDto> OccupiedFields, int TotalFields);

public sealed class GetFieldStatuses(IPassiveMembers repo, IFloorPlanSettings settings)
{
    public async Task<FieldStatusesResult> ExecuteAsync()
    {
        var special = (await settings.GetAsync()).SpecialFields;
        var occupied = await repo.GetOccupiedFieldsAsync();
        var fields = occupied
            .Select(f => new FieldStatusDto(f.Field.Value, f.DisplayName, special.GetLabel(f.Field.Value)))
            .ToList();
        return new FieldStatusesResult(fields, TotalFields: FloorGrid.TotalFields);
    }
}
