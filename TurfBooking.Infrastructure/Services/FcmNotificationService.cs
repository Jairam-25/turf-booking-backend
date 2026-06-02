using Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class FcmNotificationService : IFcmNotificationService
{
    private readonly ILogger<FcmNotificationService> _logger;

    public FcmNotificationService(ILogger<FcmNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendPushNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        if (string.IsNullOrEmpty(fcmToken))
        {
            _logger.LogWarning("Cannot send push notification. FCM Token is empty.");
            return false;
        }

        try
        {
            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data ?? new Dictionary<string, string>()
            };

            // This will use the default FirebaseApp instance initialized in Program.cs
            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Successfully sent FCM message: {Response}", response);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM push notification to token {Token}", fcmToken);
            return false;
        }
    }
}
