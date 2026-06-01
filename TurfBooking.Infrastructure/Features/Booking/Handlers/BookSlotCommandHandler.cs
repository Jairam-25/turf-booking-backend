using Application.Common.Result;
using Application.Features.Booking.Commands;
using Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Features.Booking.Handlers
{
    public sealed class BookSlotCommandHandler : IRequestHandler<BookSlotCommand, Result<object>>
    {
        private readonly IBookingService _bookingService;

        public BookSlotCommandHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<Result<object>> Handle(BookSlotCommand request, CancellationToken cancellationToken)
        {
            return await _bookingService.BookSlotAsync(request.Request, request.UserId, cancellationToken);
        }
    }
}
