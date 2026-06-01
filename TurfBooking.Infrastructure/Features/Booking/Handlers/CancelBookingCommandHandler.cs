using Application.Common.Result;
using Application.Features.Booking.Commands;
using Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Features.Booking.Handlers
{
    public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Result<string>>
    {
        private readonly IBookingService _bookingService;

        public CancelBookingCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<Result<string>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            return await _bookingService.CancelBookingAsync(request.BookingId, request.UserId, request.Reason, cancellationToken);
        }
    }
}
