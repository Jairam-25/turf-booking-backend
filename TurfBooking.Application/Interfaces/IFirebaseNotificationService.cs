using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IFirebaseNotificationService
    {
        Task<bool> SendPushNotificationAsync(string fcmToken, string title, string body, CancellationToken cancellationToken = default);
    }
}
