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
}