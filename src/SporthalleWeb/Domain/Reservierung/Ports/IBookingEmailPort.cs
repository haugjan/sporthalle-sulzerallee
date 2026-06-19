namespace SporthalleWeb.Domain.Reservierung.Ports;

public interface IBookingEmailPort
{
    Task SendProvisionConfirmationToRenterAsync(BookingSlot slot, HallMember member);
    Task SendAdminNewBookingNotificationAsync(BookingSlot slot, HallMember member);
    Task SendBookingConfirmedToRenterAsync(BookingSlot slot, HallMember member);
    Task SendBookingRejectedToRenterAsync(BookingSlot slot, HallMember member);
    Task SendRegistrationConfirmationWithMagicLinkAsync(HallMember member, string magicLinkUrl);
    Task SendMagicLinkAsync(HallMember member, string magicLinkUrl);
    Task SendPasswordResetAsync(HallMember member, string resetUrl);
}
