using Application.Common.Result;
using Application.DTOs;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResponseDto>>;
