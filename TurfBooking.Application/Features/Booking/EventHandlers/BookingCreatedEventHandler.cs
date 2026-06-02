using Application.Interfaces;
using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Booking.EventHandlers;

public class BookingCreatedEventHandler : INotificationHandler<BookingCreatedEvent>
{
    private readonly ILogger<BookingCreatedEventHandler> _logger;
    private readonly IFcmNotificationService _fcmService;
    private readonly IUserRepository _userRepository;

    public BookingCreatedEventHandler(
        ILogger<BookingCreatedEventHandler> logger,
        IFcmNotificationService fcmService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _fcmService = fcmService;
        _userRepository = userRepository;
    }

    public async Task Handle(BookingCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation("BookingCreatedEvent handled for BookingId: {BookingId}", notification.BookingId);

        // Fetch user details to get their FCM Token
        var user = await _userRepository.GetByIdAsync(notification.UserId);
        
        if (user != null && !string.IsNullOrEmpty(user.FcmToken))
        {
            // Send automatic push notification to the user's phone/browser
            await _fcmService.SendPushNotificationAsync(
                fcmToken: user.FcmToken,
                title: "⚽ Turf Booking Confirmed!",
                body: $"Your match on {notification.BookingDate:dd MMM yyyy} is confirmed. Get ready to play!"
            );
            _logger.LogInformation("Auto-sent Push Notification to UserId {UserId}", notification.UserId);
        }
    }
}
