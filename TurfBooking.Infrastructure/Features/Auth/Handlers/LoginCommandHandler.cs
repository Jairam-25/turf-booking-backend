using Application.Common.Result;
using Application.DTOs;
using Application.Features.Auth.Commands;
using Application.Interfaces;
using MediatR;

namespace Infrastructure.Features.Auth.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponseDto>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<LoginResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return _authService.LoginAsync(request.Request);
    }
}
