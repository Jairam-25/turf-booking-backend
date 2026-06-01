using Application.Common.Settings;
using Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    // RETRY POLICY — Exponential Backoff
    // Attempt 1: wait 2s, Attempt 2: wait 4s, Attempt 3: wait 8s
    private readonly AsyncRetryPolicy _retryPolicy;

    // CIRCUIT BREAKER — Stop retrying after 5 continuous failures
    // Break for 1 minute, then allow one test request through
    private static readonly AsyncCircuitBreakerPolicy _circuitBreaker =
        Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1));

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (ex, delay, attempt, _) =>
                    _logger.LogWarning(ex,
                        "Email retry {Attempt} after {Delay}s",
                        attempt, delay.TotalSeconds));
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
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Welcome to TurfXpert</title>
</head>

<body style='margin:0; padding:0; background-color:#f4f6f9; font-family: Arial, sans-serif;'>

    <table width='100%' cellpadding='0' cellspacing='0'
           style='background-color:#f4f6f9; padding:40px 0;'>

        <tr>
            <td align='center'>

                <table width='600' cellpadding='0' cellspacing='0'
                       style='background-color:#ffffff;
                              border-radius:10px;
                              overflow:hidden;
                              box-shadow:0 4px 12px rgba(0,0,0,0.08);'>

                    <!-- Header -->
                    <tr>
                        <td style='background-color:#7b39fc;
                                   padding:30px 40px;
                                   text-align:center;'>

                            <h1 style='color:#ffffff;
                                       margin:0;
                                       font-size:28px;
                                       letter-spacing:1px;'>

                                ⚽ Welcome to TurfXpert
                            </h1>

                            <p style='color:#e0d4ff;
                                      margin:8px 0 0;
                                      font-size:14px;'>

                                Your Game. Your Ground. Your Time.
                            </p>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style='padding:40px;'>

                            <h2 style='color:#7b39fc;
                                       margin-top:0;'>

                                Hi {userName}, 👋
                            </h2>

                            <p style='color:#555555;
                                      font-size:15px;
                                      line-height:1.8;'>

                                Your account has been successfully created.
                                You're now officially part of the TurfXpert community.
                            </p>

                            <p style='color:#555555;
                                      font-size:15px;
                                      line-height:1.8;'>

                                You can now:
                            </p>

                            <ul style='color:#555555;
                                       font-size:15px;
                                       line-height:2;
                                       padding-left:20px;'>

                                <li>⚽ Discover nearby turfs</li>
                                <li>📅 Book grounds instantly</li>
                                <li>👥 Organize matches with friends</li>
                                <li>🏟️ Manage your upcoming games easily</li>
                            </ul>

                            <!-- Highlight Box -->
                            <table width='100%' cellpadding='0' cellspacing='0'
                                   style='margin:25px 0;'>

                                <tr>
                                    <td style='background-color:#f3e8ff;
                                               border-left:4px solid #7b39fc;
                                               padding:16px;
                                               border-radius:6px;'>

                                        <p style='margin:0;
                                                  color:#7b39fc;
                                                  font-size:14px;
                                                  line-height:1.7;'>

                                            🚀 Start exploring available turfs
                                            and book your next match in just a few clicks.
                                        </p>
                                    </td>
                                </tr>
                            </table>

                            <p style='color:#555555;
                                      font-size:14px;
                                      line-height:1.8;'>

                                We're excited to help you enjoy a smoother
                                and faster turf booking experience.
                            </p>

                            <p style='color:#555555;
                                      font-size:14px;
                                      line-height:1.8;'>

                                See you on the field! 🏆
                            </p>

                        </td>
                    </tr>

                    <!-- Divider -->
                    <tr>
                        <td style='padding:0 40px;'>
                            <hr style='border:none;
                                       border-top:1px solid #eeeeee;' />
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style='padding:24px 40px;
                                   text-align:center;'>

                            <p style='color:#aaaaaa;
                                      font-size:12px;
                                      margin:0 0 8px;'>

                                Need help? Contact us at
                                <a href='mailto:support@turfbook.com'
                                   style='color:#7b39fc;
                                          text-decoration:none;'>

                                    support@turfbook.com
                                </a>
                            </p>

                            <p style='color:#cccccc;
                                      font-size:11px;
                                      margin:0;'>

                                © {DateTime.Now.Year} TurfXpert.
                                All rights reserved.
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

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Reset Your Password – TurfXpert";
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
                                <td style='background-color:#7b39fc; padding: 30px 40px; text-align:center;'>
                                    <h1 style='color:#ffffff; margin:0; font-size:26px; letter-spacing:1px;'>⚽ TurfXpert</h1>
                                    <p style='color:#e0d4ff; margin:6px 0 0; font-size:13px;'>Your Game. Your Ground. Your Time.</p>
                                </td>
                            </tr>

                            <!-- Body -->
                            <tr>
                                <td style='padding: 40px 40px 20px;'>
                                    <h2 style='color:#7b39fc; margin:0 0 10px;'>Hi {userName},</h2>
                                    <p style='color:#555555; font-size:15px; line-height:1.7; margin:0 0 20px;'>
                                        We received a request to reset the password for your <strong>TurfXpert</strong> account.
                                        If you made this request, click the button below to set a new password.
                                    </p>

                                    <!-- CTA Button -->
                                    <table width='100%' cellpadding='0' cellspacing='0'>
                                        <tr>
                                            <td align='center' style='padding: 20px 0;'>
                                                <a href='{resetLink}'
                                                   style='background-color:#7b39fc; color:#ffffff; text-decoration:none;
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
                                    <p style='background:#f3e8ff; padding:12px 16px; border-radius:6px; font-size:13px;
                                              color:#7b39fc; word-break:break-all; border-left: 4px solid #7b39fc;'>
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
                                        <a href='mailto:support@turfbook.com' style='color:#7b39fc; text-decoration:none;'>
                                            support@turfbook.com
                                        </a>
                                    </p>
                                    <p style='color:#cccccc; font-size:11px; margin:0;'>
                                        © {DateTime.Now.Year} TurfXpert. All rights reserved.<br/>
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

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendBookingCancellationEmailAsync(string toEmail, string userName, string turfName, DateTime bookingDate, string reason)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Booking Cancelled – TurfXpert";

        email.Body = new TextPart("html")
        {
            Text = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
            <title>Booking Cancelled</title>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f6f9; font-family: Arial, sans-serif;'>

            <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f9; padding: 40px 0;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:10px; overflow:hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);'>

                            <!-- Header -->
                            <tr>
                                <td style='background-color:#7b39fc; padding: 30px 40px; text-align:center;'>
                                    <h1 style='color:#ffffff; margin:0; font-size:26px; letter-spacing:1px;'>⚽ TurfXpert</h1>
                                    <p style='color:#e0d4ff; margin:6px 0 0; font-size:13px;'>Booking Cancellation Notice</p>
                                </td>
                            </tr>

                            <!-- Body -->
                            <tr>
                                <td style='padding: 40px 40px 20px;'>
                                    <h2 style='color:#7b39fc; margin:0 0 10px;'>Hi {userName},</h2>
                                    <p style='color:#555555; font-size:15px; line-height:1.7; margin:0 0 20px;'>
                                        Your booking for <strong>{turfName}</strong> on <strong>{bookingDate:f}</strong> has been cancelled.
                                    </p>

                                    <!-- Reason Box -->
                                    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 24px 0;'>
                                        <tr>
                                            <td style='background-color:#f3e8ff; border-left:4px solid #7b39fc; padding:14px 16px; border-radius:4px;'>
                                                <p style='margin:0; font-size:14px; color:#5b21b6;'>
                                                    <strong>Reason for cancellation:</strong><br/>
                                                    {reason}
                                                </p>
                                            </td>
                                        </tr>
                                    </table>

                                    <p style='color:#555555; font-size:14px; line-height:1.7;'>
                                        If this cancellation was a mistake, or if you wish to book another slot, please visit our website to make a new booking.
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
                                        <a href='mailto:support@turfbook.com' style='color:#7b39fc; text-decoration:none;'>
                                            support@turfbook.com
                                        </a>
                                    </p>
                                    <p style='color:#cccccc; font-size:11px; margin:0;'>
                                        © {DateTime.Now.Year} TurfXpert. All rights reserved.
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

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Your TurfXpert Verification OTP";

        email.Body = new TextPart("html")
        {
            Text = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
            <title>Verification OTP</title>
        </head>
        <body style='margin:0; padding:0; background-color:#f4f6f9; font-family: Arial, sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f6f9; padding: 40px 0;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; border-radius:10px; overflow:hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08);'>
                            <tr>
                                <td style='background-color:#7b39fc; padding: 30px 40px; text-align:center;'>
                                    <h1 style='color:#ffffff; margin:0; font-size:26px; letter-spacing:1px;'>⚽ TurfXpert</h1>
                                    <p style='color:#e0d4ff; margin:6px 0 0; font-size:13px;'>Secure Verification Code</p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 40px 40px 20px;'>
                                    <h2 style='color:#7b39fc; margin:0 0 10px;'>Hello,</h2>
                                    <p style='color:#555555; font-size:15px; line-height:1.7; margin:0 0 20px;'>
                                        Use the following one-time password (OTP) to complete your login or registration. This OTP is valid for <strong>5 minutes</strong>.
                                    </p>
                                    <table align='center' cellpadding='0' cellspacing='0' style='margin: 30px auto;'>
                                        <tr>
                                            <td style='background-color:#f3e8ff; border: 2px dashed #7b39fc; padding: 15px 40px; border-radius: 8px; text-align: center;'>
                                                <span style='font-size: 32px; font-weight: bold; color: #7b39fc; letter-spacing: 5px;'>{otpCode}</span>
                                            </td>
                                        </tr>
                                    </table>
                                    <p style='color:#555555; font-size:14px; line-height:1.7;'>
                                        If you did not request this OTP, please ignore this email or contact support.
                                    </p>
                                </td>
                            </tr>
                            <tr>
                                <td style='padding: 24px 40px; text-align:center;'>
                                    <p style='color:#cccccc; font-size:11px; margin:0;'>
                                        © {DateTime.Now.Year} TurfXpert. All rights reserved.
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

        await SendEmailWithResilienceAsync(email);
    }

    // CORE — Send email wrapped in Retry + Circuit Breaker
    private async Task SendEmailWithResilienceAsync(MimeMessage email)
    {
        // Wrap retry INSIDE circuit breaker
        // Circuit breaker checks first — if open, throws immediately
        // If closed/half-open, retry policy handles transient failures
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, false);
                await smtp.AuthenticateAsync(
                    _emailSettings.Email,
                    _emailSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    "Email sent successfully to {To}",
                    email.To);
            });
        });
    }
}

