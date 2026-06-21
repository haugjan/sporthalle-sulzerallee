namespace SporthalleWeb.Presentation.Booking.Dtos;

public sealed record AdminBookingResponse(BookingSlotDto Slot, HallMemberDto? Member);
