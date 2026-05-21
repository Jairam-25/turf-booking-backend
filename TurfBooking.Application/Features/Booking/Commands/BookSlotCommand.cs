using Application.Common.Result;
using Application.DTOs;
using MediatR;

namespace Application.Features.Booking.Commands
{
    public sealed record BookSlotCommand(CreateBookingDto Request, int UserId) : IRequest<Result<object>>;
}
