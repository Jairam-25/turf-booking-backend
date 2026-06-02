using Application.Interfaces;
using Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Features.Booking.EventHandlers;

public class BookingCreatedEventHandler : INotificationHandler<BookingCreatedEvent>
{
    private readonly ILogger<BookingCreatedEventHandler> _logger;
    private readonly IUserRepository _userRepository;
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;

    public BookingCreatedEventHandler(
        ILogger<BookingCreatedEventHandler> logger,
        IUserRepository userRepository,
        Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _userRepository = userRepository;
        _scopeFactory = scopeFactory;
    }

    public async Task Handle(BookingCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("BookingCreatedEvent handled for BookingId: {BookingId}", notification.BookingId);

        // Fetch user details to get their FCM Token
        var user = await _userRepository.GetByIdAsync(notification.UserId);
        
        if (user != null && !string.IsNullOrEmpty(user.FcmToken))
        {
            var fcmToken = user.FcmToken;
            var dateStr = notification.BookingDate.ToString("dd MMM yyyy");
            var userId = notification.UserId;

            // Send automatic push notification in background to avoid delaying API response
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var fcmService = scope.ServiceProvider.GetRequiredService<IFcmNotificationService>();
                
                try
                {
                    await fcmService.SendPushNotificationAsync(
                        fcmToken: fcmToken,
                        title: "⚽ Turf Booking Confirmed!",
                        body: $"Your match on {dateStr} is confirmed. Get ready to play!"
                    );
                    _logger.LogInformation("Auto-sent Push Notification to UserId {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send background FCM notification for UserId {UserId}", userId);
                }
            });
        }
    }
}
