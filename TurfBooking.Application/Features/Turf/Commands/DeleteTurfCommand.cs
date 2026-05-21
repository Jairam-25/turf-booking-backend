using MediatR;

namespace Application.Features.Turf.Commands
{
    public sealed record DeleteTurfCommand(int Id) : IRequest<bool>;
}
