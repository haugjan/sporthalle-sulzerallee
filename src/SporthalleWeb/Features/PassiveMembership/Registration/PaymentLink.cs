namespace SporthalleWeb.Features.PassiveMembership.Registration;

/// <summary>
/// External RaiseNow payment page for the passive-membership yearly fee.
/// Referenced by the registration wizard (redirect right after sign-up) and by the
/// confirmation email (payment call-to-action).
/// </summary>
public static class PaymentLink
{
    public const string Url = "https://pay.raisenow.io/zbnwc";
}
