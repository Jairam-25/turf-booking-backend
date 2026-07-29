using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;
            EnsureFirebaseInitialized();
        }

        private void EnsureFirebaseInitialized()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                try
                {
                    string credentialPath = System.IO.Path.Combine(AppContext.BaseDirectory, "firebase-service-account.json");
                    if (!System.IO.File.Exists(credentialPath))
                    {
                        credentialPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "firebase-service-account.json");
                    }
                    if (System.IO.File.Exists(credentialPath))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(credentialPath)
                        });
                        _logger.LogInformation("FirebaseApp initialized successfully.");
                    }
                    else
                    {
                        _logger.LogWarning("Firebase credential file not found at {Path}", credentialPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize FirebaseApp.");
                }
            }
        }

        public async Task<bool> SendPushNotificationAsync(string fcmToken, string title, string body, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fcmToken))
                return false;

            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("Firebase is not initialized. Cannot send push notification.");
                return false;
            }

            var message = new Message()
            {
                Token = fcmToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                }
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);
                _logger.LogInformation("Successfully sent message: {Response}", response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Firebase push notification.");
                return false;
            }
        }
    }
}
