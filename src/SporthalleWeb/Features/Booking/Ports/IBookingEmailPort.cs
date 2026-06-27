using SporthalleWeb.Domain.Booking.HallMemberAggregate;
using SporthalleWeb.Domain.Booking.SlotAggregate;

namespace SporthalleWeb.Features.Booking.Ports;

public interface IBookingEmail
{
    Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null);
    Task SendAdminNewBookingNotificationAsync(BookingSlot slot, HallMember member);
    Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null);
    Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member, string? customEmailBody = null);
    Task SendRegistrationConfirmationWithMagicLinkAsync(HallMember member, string magicLinkUrl);
    Task SendMagicLinkAsync(HallMember member, string magicLinkUrl);
    Task SendPasswordResetAsync(HallMember member, string resetUrl);
}
