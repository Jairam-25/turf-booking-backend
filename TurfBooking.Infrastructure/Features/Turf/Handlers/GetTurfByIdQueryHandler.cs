using Application.DTOs;
using Application.Features.Turf.Queries;
using MediatR;
using Persistence.Interfaces;

namespace Infrastructure.Features.Turf.Handlers;

public sealed class GetTurfByIdQueryHandler : IRequestHandler<GetTurfByIdQuery, TurfResponseDto?>
{
    private readonly ITurfService _turfService;

    public GetTurfByIdQueryHandler(ITurfService turfService) => _turfService = turfService;

    public Task<TurfResponseDto?> Handle(GetTurfByIdQuery request, CancellationToken cancellationToken)
    {
        return _turfService.GetTurfByIdAsync(request.Id);
    }
}
