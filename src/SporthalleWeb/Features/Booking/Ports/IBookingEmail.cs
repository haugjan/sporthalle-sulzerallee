using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Features.Booking.Ports;

public interface IBookingEmail
{
    Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null);
    Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null);
    Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null);
}
