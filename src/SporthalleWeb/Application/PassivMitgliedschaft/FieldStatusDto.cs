namespace SporthalleWeb.Application.PassivMitgliedschaft;

public record FieldStatusDto(int FieldNumber, string? DisplayName, string? VipLabel);

public record FieldStatusesResult(IReadOnlyList<FieldStatusDto> OccupiedFields, int TotalFields);
