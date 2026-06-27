namespace SporthalleWeb.Infrastructure.Shared;

/// <summary>
/// Single source of truth for the visual layout of all system e-mails.
/// Produces self-contained HTML (sent via Brevo's <c>htmlContent</c>, no Brevo template).
/// Design follows the homepage: Dust red (#EB504B), ink (#101010), Manrope / Anton.
/// Reference markup: <c>docs/email/brevo-universal-template.html</c>.
/// </summary>
public static class EmailLayout
{
    private const string LogoUrl =
        "https://app-sporthalle-sulzerallee.azurewebsites.net/img/sporthalle_sulzerallee_logo_neu.png";

    /// <param name="title">Heading shown in the mail (required).</param>
    /// <param name="body">Main text. Plain text with line breaks or simple HTML.</param>
    /// <param name="greeting">Optional salutation line (e.g. "Guten Tag Max Muster").</param>
    /// <param name="details">Optional detail box (e.g. summary/fields).</param>
    /// <param name="note">Optional muted secondary note below the body.</param>
    /// <param name="ctaUrl">Optional call-to-action button URL.</param>
    /// <param name="ctaLabel">Optional button label (default "Mehr erfahren").</param>
    public static string Render(
        string title,
        string body,
        string? greeting = null,
        string? details = null,
        string? note = null,
        string? ctaUrl = null,
        string? ctaLabel = null)
    {
        var bodyHtml = (body ?? "").Replace("\n", "<br>");
        var detailsHtml = string.IsNullOrWhiteSpace(details) ? null : details!.Replace("\n", "<br>");

        var greetingBlock = string.IsNullOrWhiteSpace(greeting)
            ? ""
            : $"<p style=\"margin:0 0 14px;font-size:16px;color:#101010;\">{greeting}</p>";

        var detailsBlock = detailsHtml is null
            ? ""
            : $"""
              <tr><td style="padding:22px 40px 0;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f5f5f5;border-left:4px solid #EB504B;border-radius:4px;">
                  <tr><td style="padding:16px 18px;font-family:'Manrope',Arial,Helvetica,sans-serif;color:#323232;font-size:15px;line-height:1.6;">{detailsHtml}</td></tr>
                </table>
              </td></tr>
              """;

        var noteBlock = string.IsNullOrWhiteSpace(note)
            ? ""
            : $"""<tr><td style="padding:18px 40px 0;font-family:'Manrope',Arial,Helvetica,sans-serif;color:#666666;font-size:14px;line-height:1.6;">{note}</td></tr>""";

        var ctaBlock = string.IsNullOrWhiteSpace(ctaUrl)
            ? ""
            : $"""
              <tr><td align="left" style="padding:28px 40px 0;">
                <table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>
                  <td style="background:#EB504B;border-radius:6px;">
                    <a href="{ctaUrl}" target="_blank" style="display:inline-block;padding:13px 28px;font-family:'Manrope',Arial,Helvetica,sans-serif;font-weight:700;font-size:15px;color:#ffffff;text-decoration:none;">{(string.IsNullOrWhiteSpace(ctaLabel) ? "Mehr erfahren" : ctaLabel)}</a>
                  </td>
                </tr></table>
              </td></tr>
              """;

        return $"""
        <!DOCTYPE html>
        <html lang="de" xmlns="http://www.w3.org/1999/xhtml">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <meta name="x-apple-disable-message-reformatting">
        </head>
        <body style="margin:0;padding:0;background:#f4f4f4;">
          <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{title}</div>
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f4f4f4;">
            <tr><td align="center" style="padding:32px 12px;">
              <table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px;background:#ffffff;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;">
                <tr><td style="height:6px;line-height:6px;font-size:6px;background:#EB504B;">&nbsp;</td></tr>
                <tr><td align="center" style="padding:28px 40px 4px;">
                  <img src="{LogoUrl}" alt="Sporthalle Sulzerallee" width="190" style="display:block;width:190px;max-width:60%;height:auto;">
                </td></tr>
                <tr><td style="padding:20px 40px 0;">
                  <h1 style="margin:0;font-family:'Anton','Arial Black',Impact,sans-serif;font-weight:400;text-transform:uppercase;letter-spacing:0.5px;color:#101010;font-size:30px;line-height:1.15;">{title}</h1>
                  <div style="height:3px;width:48px;background:#EB504B;margin-top:14px;"></div>
                </td></tr>
                <tr><td style="padding:18px 40px 0;font-family:'Manrope',Arial,Helvetica,sans-serif;color:#323232;font-size:16px;line-height:1.65;">
                  {greetingBlock}
                  <div>{bodyHtml}</div>
                </td></tr>
                {detailsBlock}
                {noteBlock}
                {ctaBlock}
                <tr><td style="padding:34px 0 0;">&nbsp;</td></tr>
                <tr><td style="border-top:1px solid #f0f0f0;font-size:0;line-height:0;">&nbsp;</td></tr>
                <tr><td align="center" style="padding:22px 40px 32px;font-family:'Manrope',Arial,Helvetica,sans-serif;color:#9ca3af;font-size:13px;line-height:1.7;">
                  <span style="font-family:'Anton','Arial Black',Impact,sans-serif;text-transform:uppercase;letter-spacing:0.5px;color:#101010;font-size:15px;">Sporthalle Sulzerallee</span><br>
                  Winterthur<br>
                  <a href="https://www.sporthalle-sulzerallee.ch" target="_blank" style="color:#EB504B;text-decoration:none;">www.sporthalle-sulzerallee.ch</a>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
