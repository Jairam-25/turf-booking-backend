using Application.Common.Settings;
using Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string userName)
    {
        var email = new MimeMessage();

        email.From.Add(
            MailboxAddress.Parse(_emailSettings.Email));

        email.To.Add(
            MailboxAddress.Parse(toEmail));

        email.Subject = "Account Created Successfully";

        email.Body = new TextPart("html")
        {
            Text = $@"
                <h2>Welcome {userName}</h2>

                <p>Your account has been created successfully.</p>

                <p>Welcome to Turf Booking App.</p>
            "
        };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            "smtp.gmail.com",
            587,
            false);

        await smtp.AuthenticateAsync(
            _emailSettings.Email,
            _emailSettings.Password);

        await smtp.SendAsync(email);

        await smtp.DisconnectAsync(true);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Reset Your Password – TurfBook";
        var resetLink = _emailSettings.ResetPasswordUrl + resetToken;

        email.Body = new TextPart("html")
        {
            Text = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
            <title>Reset Your Password</title>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f6f9; font-family: Arial, sans-serif;'>

            <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f9; padding: 40px 0;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:10px; overflow:hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);'>

                            <!-- Header -->
                            <tr>
                                <td style='background-color:#1a7a4a; padding: 30px 40px; text-align:center;'>
                                    <h1 style='color:#ffffff; margin:0; font-size:26px; letter-spacing:1px;'>⚽ TurfBook</h1>
                                    <p style='color:#a8e6c1; margin:6px 0 0; font-size:13px;'>Your Game. Your Ground. Your Time.</p>
                                </td>
                            </tr>

                            <!-- Body -->
                            <tr>
                                <td style='padding: 40px 40px 20px;'>
                                    <h2 style='color:#1a7a4a; margin:0 0 10px;'>Hi {userName},</h2>
                                    <p style='color:#555555; font-size:15px; line-height:1.7; margin:0 0 20px;'>
                                        We received a request to reset the password for your <strong>TurfBook</strong> account.
                                        If you made this request, click the button below to set a new password.
                                    </p>

                                    <!-- CTA Button -->
                                    <table width='100%' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td align='center' style='padding: 20px 0;'>
                                                <a href='{resetLink}'
                                                   style='background-color:#1a7a4a; color:#ffffff; text-decoration:none;
                                                          padding:14px 36px; border-radius:6px; font-size:16px;
                                                          font-weight:bold; display:inline-block; letter-spacing:0.5px;'>
                                                    🔑 Reset My Password
                                                </a>
                                            </td>
                                        </tr>
                                    </table>

                                    <p style='color:#555555; font-size:14px; line-height:1.7;'>
                                        Or copy and paste this link into your browser:
                                    </p>
                                    <p style='background:#f0f8f4; padding:12px 16px; border-radius:6px; font-size:13px;
                                              color:#1a7a4a; word-break:break-all; border-left: 4px solid #1a7a4a;'>
                                        {resetLink}
                                    </p>

                                    <!-- Warning -->
                                    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 24px 0;'>
                                        <tr>
                                            <td style='background-color:#fff8e1; border-left:4px solid #f5a623;
                                                        padding:14px 16px; border-radius:4px;'>
                                                <p style='margin:0; font-size:13px; color:#7a5c00;'>
                                                    ⚠️ <strong>This link will expire in 30 minutes.</strong>
                                                    If you did not request a password reset, please ignore this email
                                                    or contact our support team immediately.
                                                </p>
                                            </td>
                                        </tr>
                                    </table>

                                    <p style='color:#555555; font-size:14px; line-height:1.7;'>
                                        After resetting your password, you can continue booking your favourite turfs,
                                        managing your upcoming matches, and enjoying seamless ground reservations. 🏟️
                                    </p>
                                </td>
                            </tr>

                            <!-- Divider -->
                            <tr>
                                <td style='padding: 0 40px;'>
                                    <hr style='border:none; border-top:1px solid #eeeeee;' />
                                </td>
                            </tr>

                            <!-- Footer -->
                            <tr>
                                <td style='padding: 24px 40px; text-align:center;'>
                                    <p style='color:#aaaaaa; font-size:12px; margin:0 0 8px;'>
                                        Need help? Contact us at
                                        <a href='mailto:support@turfbook.com' style='color:#1a7a4a; text-decoration:none;'>
                                            support@turfbook.com
                                        </a>
                                    </p>
                                    <p style='color:#cccccc; font-size:11px; margin:0;'>
                                        © {DateTime.Now.Year} TurfBook. All rights reserved.<br/>
                                        You're receiving this email because a password reset was requested for your account.
                                    </p>
                                </td>
                            </tr>

                        </table>
                    </td>
                </tr>
            </table>

        </body>
        </html>"
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, false);
        await smtp.AuthenticateAsync(_emailSettings.Email, _emailSettings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}