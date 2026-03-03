namespace Api.Features.Platform;

internal sealed class WelcomeEmailBuilder : IWelcomeEmailBuilder
{
    public WelcomeEmail Build(WelcomeEmailData data)
    {
        var subject = $"Welcome to ZenPharm — {data.PharmacyName} is ready!";

        var htmlBody = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1.0" /></head>
            <body style="margin:0;padding:0;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:#f5f5f5;">
              <table width="100%" cellpadding="0" cellspacing="0" style="max-width:600px;margin:0 auto;background:#ffffff;">
                <tr>
                  <td style="background:#1a1a2e;padding:32px;text-align:center;">
                    <h1 style="color:#ffffff;margin:0;font-size:24px;">ZenPharm</h1>
                  </td>
                </tr>
                <tr>
                  <td style="padding:32px;">
                    <h2 style="color:#1a1a2e;margin:0 0 16px;">Welcome, {{data.AdminFullName}}!</h2>
                    <p style="color:#333;line-height:1.6;">
                      Your pharmacy <strong>{{data.PharmacyName}}</strong> has been set up and is ready to use.
                    </p>

                    <table width="100%" style="background:#f8f9fa;border-radius:8px;margin:24px 0;border-collapse:collapse;">
                      <tr>
                        <td style="padding:16px;">
                          <p style="margin:0 0 8px;color:#666;font-size:13px;text-transform:uppercase;letter-spacing:1px;">Your Login Details</p>
                          <p style="margin:0 0 4px;color:#333;"><strong>Email:</strong> {{data.AdminEmail}}</p>
                          <p style="margin:0 0 4px;color:#333;"><strong>Temporary Password:</strong> <code style="background:#e9ecef;padding:2px 6px;border-radius:4px;">{{data.TemporaryPassword}}</code></p>
                          <p style="margin:0;color:#333;"><strong>Plan:</strong> {{data.PlanName}} ({{data.BillingPeriod}})</p>
                        </td>
                      </tr>
                    </table>

                    <p style="color:#dc3545;font-size:14px;margin:0 0 24px;">
                      Please change your password after your first login.
                    </p>

                    <table width="100%" cellpadding="0" cellspacing="0">
                      <tr>
                        <td align="center">
                          <a href="{{data.AdminPanelUrl}}"
                             style="display:inline-block;background:#1a1a2e;color:#ffffff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:600;font-size:16px;">
                            Log In to Your Admin Panel
                          </a>
                        </td>
                      </tr>
                    </table>

                    <p style="color:#666;font-size:14px;margin:24px 0 0;line-height:1.6;">
                      If you have any questions, reply to this email or contact us at
                      <a href="mailto:support@zenpharm.com.au" style="color:#1a1a2e;">support@zenpharm.com.au</a>.
                    </p>
                  </td>
                </tr>
                <tr>
                  <td style="background:#f8f9fa;padding:24px;text-align:center;color:#999;font-size:12px;">
                    &copy; {{DateTimeOffset.UtcNow.Year}} ZenPharm by Zentech Consulting. All rights reserved.
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;

        return new WelcomeEmail(subject, htmlBody);
    }
}
