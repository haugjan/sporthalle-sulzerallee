using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Application.Reservierung;

public sealed class RequestPasswordResetUseCase(
    IMemberManagerPort members,
    IBookingEmailPort email)
{
    public async Task ExecuteAsync(string emailRaw)
    {
        var member = await members.FindByEmailAsync(emailRaw.Trim().ToLowerInvariant());
        if (member is null) return;

        var lastSent = await members.GetPasswordResetSentAtAsync(member.Id);
        if (lastSent.HasValue && (DateTime.UtcNow - lastSent.Value).TotalMinutes < 10)
            return;

        var resetToken = await members.GeneratePasswordResetTokenAsync(member.Id);
        var resetUrl = "https://www.sporthalle-sulzerallee.ch/reservierung/reset-password" +
                       $"?token={Uri.EscapeDataString(resetToken)}&id={member.Id}";

        await members.SetPasswordResetSentAtAsync(member.Id, DateTime.UtcNow);
        await email.SendPasswordResetAsync(member, resetUrl);
    }
}
