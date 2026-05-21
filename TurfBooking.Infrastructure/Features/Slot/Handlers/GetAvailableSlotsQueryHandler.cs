using Application.Common.Result;
using Application.DTOs;
using Application.Features.Slot.Queries;
using Application.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Features.Slot.Handlers
{
    public sealed class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, Result<IEnumerable<SlotResponseDto>>>
    {
        private readonly ISlotService _slotService;

        public GetAvailableSlotsQueryHandler(ISlotService slotService)
        {
            _slotService = slotService;
        }

        public async Task<Result<IEnumerable<SlotResponseDto>>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
        {
            return await _slotService.GetAvailableSlotsAsync(request.TurfId, cancellationToken);
        }
    }
}
