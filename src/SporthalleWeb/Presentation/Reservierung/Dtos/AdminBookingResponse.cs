namespace SporthalleWeb.Presentation.Reservierung.Dtos;

public sealed record AdminBookingResponse(BookingSlotDto Slot, HallMemberDto? Member);
