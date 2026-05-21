using Application.DTOs;
using MediatR;

namespace Application.Features.Turf.Commands
{
    public sealed record CreateTurfCommand(CreateTurfDto Dto) : IRequest<TurfResponseDto>;
}
