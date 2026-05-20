using Application.DTOs;
using Application.Model;
using MediatR;

namespace Application.Features.Turf.Queries;

public sealed record GetAllTurfsQuery(TurfQueryParameters Query) : IRequest<PagedResult<TurfResponseDto>>;
