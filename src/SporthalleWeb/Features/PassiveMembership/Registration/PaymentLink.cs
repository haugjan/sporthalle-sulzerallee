namespace SporthalleWeb.Features.PassiveMembership.Registration;

public static class PaymentLink
{
    public const string Url = "https://pay.raisenow.io/zbnwc";

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
