using Application.DTOs;
using Application.Features.Turf.Queries;
using Application.Model;
using MediatR;
using Persistence.Interfaces;

namespace Infrastructure.Features.Turf.Handlers;

public sealed class GetAllTurfsQueryHandler : IRequestHandler<GetAllTurfsQuery, PagedResult<TurfResponseDto>>
{
    private readonly ITurfService _turfService;

    public GetAllTurfsQueryHandler(ITurfService turfService) => _turfService = turfService;

    public Task<PagedResult<TurfResponseDto>> Handle(GetAllTurfsQuery request, CancellationToken cancellationToken)
    {
        return _turfService.GetAllTurfAsync(request.Query);
    }
}
