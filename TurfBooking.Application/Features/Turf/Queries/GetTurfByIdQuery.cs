using Application.DTOs;
using MediatR;

namespace Application.Features.Turf.Queries;

public sealed record GetTurfByIdQuery(int Id) : IRequest<TurfResponseDto?>;
