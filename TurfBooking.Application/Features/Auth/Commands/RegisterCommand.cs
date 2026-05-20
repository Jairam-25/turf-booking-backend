using Application.Common.Result;
using Application.DTOs;
using MediatR;

namespace Application.Features.Auth.Commands;

public sealed record RegisterCommand(RegisterRequestDto Request) : IRequest<Result<string>>;
