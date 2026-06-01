using Application.Common.Result;
using Application.Features.Auth.Commands;
using Application.Interfaces;
using MediatR;

namespace Infrastructure.Features.Auth.Handlers;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<string>>
{
    private readonly IAuthService _authService;

    public ForgotPasswordCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<string>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        return _authService.ForgotPasswordAsync(request.Request);
    }
}
