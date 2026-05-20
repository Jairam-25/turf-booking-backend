using Application.Common.Result;
using Application.DTOs;
using Application.Features.Turf.Queries;
using Application.Model;
using MediatR;
using Persistence.Interfaces;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace TurfBooking.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class TurfController(IMediator mediator, ITurfService turfService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ITurfService _turfService = turfService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] TurfQueryParameters query)
    {
        var result = await _mediator.Send(
            new GetAllTurfsQuery(query));

        return Ok(ApiResponse<PagedResult<TurfResponseDto>>.SuccessResponse(result, "Turfs retrieved successfully"));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(
            new GetTurfByIdQuery(id));

        if (result == null)
        {
            return NotFound(ApiResponse<object>.FailureResponse("Turf not found", null, 404));
        }

        return Ok(ApiResponse<TurfResponseDto>.SuccessResponse(result, "Turf retrieved successfully"));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTurfDto dto)
    {
        var result = await _turfService.CreateTurfAsync(dto);

        return Ok(ApiResponse<TurfResponseDto>.SuccessResponse(result, "Turf created successfully"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _turfService.DeleteTurfAsync(id);

        if (!result)
            return NotFound(ApiResponse<object>.FailureResponse("Turf not found", null, 404));

        return Ok(ApiResponse<string>.SuccessResponse(string.Empty, "Turf deleted successfully"));
    }
}