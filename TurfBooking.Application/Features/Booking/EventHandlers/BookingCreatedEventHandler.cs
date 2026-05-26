using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.Booking.EventHandlers;

public class BookingCreatedEventHandler : INotificationHandler<BookingCreatedEvent>
{
    private readonly ILogger<BookingCreatedEventHandler> _logger;

    public BookingCreatedEventHandler(ILogger<BookingCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(BookingCreatedEvent notification, CancellationToken ct)
    {
        // Send confirmation email
        // Log to analytics
        // Notify admin
        _logger.LogInformation("BookingCreatedEvent handled for BookingId: {BookingId}", notification.BookingId);

        await Task.CompletedTask;
    }
}
