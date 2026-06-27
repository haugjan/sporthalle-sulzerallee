namespace SporthalleWeb.Features.Booking;

public sealed record AdminBookingResponse(BookingSlotDto Slot, HallMemberDto? Member);
