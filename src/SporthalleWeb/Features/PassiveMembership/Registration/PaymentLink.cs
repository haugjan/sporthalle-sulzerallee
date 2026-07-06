namespace SporthalleWeb.Features.PassiveMembership.Registration;

/// <summary>
/// External RaiseNow payment page for the passive-membership yearly fee.
/// Referenced by the registration wizard (redirect right after sign-up) and by the
/// confirmation email (payment call-to-action).
/// </summary>
public static class PaymentLink
{
    public const string Url = "https://pay.raisenow.io/zbnwc";

    /// <summary>
    /// Builds the payment URL with the floor field number (and optional contact data) attached
    /// as RaiseNow custom parameters (<c>rnw-stored_*</c>). These persist on the transaction and
    /// are visible in the RaiseNow manager, so a payment can be reconciled to the correct floor
    /// field when it is marked as paid in the backoffice.
    /// </summary>
    public static string ForField(int fieldNumber, string? email = null, string? name = null)
    {
        var query = new List<string>
        {
            $"rnw-stored_fieldnumber={Uri.EscapeDataString(fieldNumber.ToString())}"
        };
        if (!string.IsNullOrWhiteSpace(email))
            query.Add($"rnw-stored_email={Uri.EscapeDataString(email.Trim())}");
        if (!string.IsNullOrWhiteSpace(name))
            query.Add($"rnw-stored_name={Uri.EscapeDataString(name.Trim())}");
        return $"{Url}?{string.Join("&", query)}";
    }
}
