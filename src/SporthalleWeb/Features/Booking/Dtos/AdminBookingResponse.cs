namespace SporthalleWeb.Features.Booking.Dtos;

public sealed record AdminBookingResponse(BookingSlotDto Slot, HallMemberDto? Member);
