namespace Application.Interfaces;

public interface IFcmNotificationService
{
    Task<bool> SendPushNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
}
