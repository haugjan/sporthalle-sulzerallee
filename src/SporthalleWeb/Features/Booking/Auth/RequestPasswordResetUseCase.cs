using SporthalleWeb.Features.Booking;

namespace SporthalleWeb.Features.Booking;

public sealed class RequestPasswordReset(
    IHallMembers members,
    IBookingEmail email)
{
    public async Task ExecuteAsync(string emailRaw)
    {
        var member = await members.FindByEmailAsync(emailRaw.Trim().ToLowerInvariant());
        if (member is null) return;

        var resetToken = await members.GeneratePasswordResetTokenAsync(member.Id);
        var resetUrl = "https://www.sporthalle-sulzerallee.ch/reservierung/reset-password" +
                       $"?token={Uri.EscapeDataString(resetToken)}&id={member.Id}";

        await email.SendPasswordResetAsync(member, resetUrl);
    }
}
