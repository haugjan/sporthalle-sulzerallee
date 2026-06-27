namespace SporthalleWeb.Features.PassiveMembership.Registration;

public record FieldStatusResponse(
    IReadOnlyList<FieldStatusItem> OccupiedFields,
    int TotalFields,
    int OccupiedCount);

public record FieldStatusItem(int FieldNumber, string? DisplayName, string? VipLabel);
