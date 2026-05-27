using Application.DTOs;
using Application.Features.Turf.Commands;
using MediatR;
using Persistence.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Features.Turf.Handlers
{
    public sealed class CreateTurfCommandHandler : IRequestHandler<CreateTurfCommand, TurfResponseDto>
    {
        private readonly ITurfService _turfService;

        public CreateTurfCommandHandler(ITurfService turfService)
        {
            _turfService = turfService;
        }

        public async Task<TurfResponseDto> Handle(CreateTurfCommand request, CancellationToken cancellationToken)
        {
            return await _turfService.CreateTurfAsync(request.Dto);
        }
    }
}
