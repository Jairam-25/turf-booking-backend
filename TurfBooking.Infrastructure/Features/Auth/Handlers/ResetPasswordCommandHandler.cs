using Application.Common.Result;
using Application.Features.Auth.Commands;
using Application.Interfaces;
using MediatR;

namespace Infrastructure.Features.Auth.Handlers;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
{
    private readonly IAuthService _authService;

    public ResetPasswordCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        return _authService.ResetPasswordAsync(request.Request);
    }
}
