namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record AdjustPriceRequest(decimal NewPricePerBlock, string? Note);
