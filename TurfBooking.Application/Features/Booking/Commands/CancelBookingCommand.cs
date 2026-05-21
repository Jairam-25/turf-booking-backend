using Application.Common.Result;
using MediatR;

namespace Application.Features.Booking.Commands
{
    public sealed record CancelBookingCommand(int BookingId, int UserId) : IRequest<Result<string>>;
}
