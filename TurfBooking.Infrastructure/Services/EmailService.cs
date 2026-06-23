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

    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Account Created Successfully";

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "WelcomeEmail.html");
        var htmlTemplate = await File.ReadAllTextAsync(templatePath);

        htmlTemplate = htmlTemplate
            .Replace("{userName}", userName)
            .Replace("{year}", DateTime.UtcNow.Year.ToString());

        email.Body = new TextPart("html") { Text = htmlTemplate };

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Reset Your Password – TurfXpert";
        
        var resetLink = _emailSettings.ResetPasswordUrl + resetToken;

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "PasswordResetEmail.html");
        var htmlTemplate = await File.ReadAllTextAsync(templatePath);

        htmlTemplate = htmlTemplate
            .Replace("{userName}", userName)
            .Replace("{resetLink}", resetLink)
            .Replace("{year}", DateTime.UtcNow.Year.ToString());

        email.Body = new TextPart("html") { Text = htmlTemplate };

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendBookingCancellationEmailAsync(string toEmail, string userName, string turfName, DateTime bookingDate, string reason)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Booking Cancelled – TurfXpert";

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "BookingCancelledEmail.html");
        var htmlTemplate = await File.ReadAllTextAsync(templatePath);

        htmlTemplate = htmlTemplate
            .Replace("{userName}", userName)
            .Replace("{turfName}", turfName)
            .Replace("{bookingDate}", bookingDate.ToString("f"))
            .Replace("{reason}", reason)
            .Replace("{year}", DateTime.UtcNow.Year.ToString());

        email.Body = new TextPart("html") { Text = htmlTemplate };

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = "Your TurfXpert Verification OTP";

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "OtpEmail.html");
        var htmlTemplate = await File.ReadAllTextAsync(templatePath);

        htmlTemplate = htmlTemplate
            .Replace("{otpCode}", otpCode)
            .Replace("{year}", DateTime.UtcNow.Year.ToString());

        email.Body = new TextPart("html") { Text = htmlTemplate };

        await SendEmailWithResilienceAsync(email);
    }

    public async Task SendAccountStatusUpdateEmailAsync(string toEmail, string userName, string status, string reason)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_emailSettings.Email));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = status == "Blocked" ? "Account Temporarily Restricted" : "Account Status Update";

        var htmlTemplate = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ padding: 20px; border: 1px solid #ddd; border-radius: 5px; max-width: 600px; margin: 0 auto; }}
                .header {{ font-size: 20px; font-weight: bold; margin-bottom: 20px; color: {(status == "Blocked" ? "#e74c3c" : "#f39c12")}; }}
                .footer {{ margin-top: 30px; font-size: 12px; color: #777; text-align: center; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>TurfXpert Account Update</div>
                <p>Hello {userName},</p>
                <p>Your TurfXpert account status has been updated to <strong>{status}</strong>.</p>
                <p><strong>Reason provided by administrator:</strong></p>
                <blockquote style='border-left: 4px solid #ddd; padding-left: 10px; color: #555;'>{reason}</blockquote>
                <p>If you believe this is a mistake, please contact our support team.</p>
                <div class='footer'>&copy; {DateTime.UtcNow.Year} TurfXpert. All rights reserved.</div>
            </div>
        </body>
        </html>";

        email.Body = new TextPart("html") { Text = htmlTemplate };

        await SendEmailWithResilienceAsync(email);
    }

    private static readonly HttpClient _httpClient = new HttpClient();

    // CORE — Send email wrapped in Retry + Circuit Breaker using Brevo API
    private async Task SendEmailWithResilienceAsync(MimeMessage email)
    {
        await _circuitBreaker.ExecuteAsync(async () =>
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                var targetEmail = email.To.Mailboxes.FirstOrDefault()?.Address;
                var subject = email.Subject;
                var htmlContent = email.Body is TextPart textPart ? textPart.Text : "Empty Content";

                var payload = new
                {
                    sender = new { name = "TurfXpert", email = "turfbookingxpert@gmail.com" },
                    to = new[] { new { email = targetEmail } },
                    subject = subject,
                    htmlContent = htmlContent
                };

                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", _emailSettings.Password);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Brevo API failed with status {response.StatusCode}: {errorDetails}");
                }

                _logger.LogInformation("Email sent successfully via Brevo API to {To}", targetEmail);
            });
        });
    }
}

