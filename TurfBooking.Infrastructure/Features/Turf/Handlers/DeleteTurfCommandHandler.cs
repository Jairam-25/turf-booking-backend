using Application.Features.Turf.Commands;
using MediatR;
using Persistence.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Features.Turf.Handlers
{
    public sealed class DeleteTurfCommandHandler : IRequestHandler<DeleteTurfCommand, bool>
    {
        private readonly ITurfService _turfService;

        public DeleteTurfCommandHandler(ITurfService turfService)
        {
            _turfService = turfService;
        }

        public async Task<bool> Handle(DeleteTurfCommand request, CancellationToken cancellationToken)
        {
            return await _turfService.DeleteTurfAsync(request.Id);
        }
    }
}
