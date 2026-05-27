using Application.Common.Result;
using Application.Features.Booking.Queries;
using Application.Interfaces;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Features.Booking.Handlers
{
    public sealed class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, Result<object>>
    {
        private readonly IBookingService _bookingService;

        public GetMyBookingsQueryHandler(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        public async Task<Result<object>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
        {
            return await _bookingService.GetMyBookingsAsync(request.UserId, cancellationToken);
        }
    }
}
