using Application.Common.Result;
using Application.DTOs;
using MediatR;
using System.Collections.Generic;

namespace Application.Features.Slot.Queries
{
    public sealed record GetAvailableSlotsQuery(int TurfId) : IRequest<Result<IEnumerable<SlotResponseDto>>>;
}
