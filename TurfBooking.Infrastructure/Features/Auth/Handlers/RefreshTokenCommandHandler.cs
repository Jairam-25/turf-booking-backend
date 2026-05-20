using Application.Common.Result;
using Application.DTOs;
using Application.Features.Auth.Commands;
using Application.Interfaces;
using MediatR;

namespace Infrastructure.Features.Auth.Handlers;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<LoginResponseDto>>
{
    private readonly IAuthService _authService;

    public RefreshTokenCommandHandler(IAuthService authService) => _authService = authService;

    public Task<Result<LoginResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return _authService.RefreshTokenAsync(request.RefreshToken);
    }
}
