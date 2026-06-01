using Application.Common.Result;
using Application.Features.Auth.Commands;
using Application.Interfaces;
using MediatR;

namespace Infrastructure.Features.Auth.Handlers;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<string>>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<string>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return _authService.RegisterAsync(request.Request);
    }
}
