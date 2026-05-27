using Application.Common.Result;
using MediatR;

namespace Application.Features.Booking.Queries
{
    public sealed record GetMyBookingsQuery(int UserId) : IRequest<Result<object>>;
}
