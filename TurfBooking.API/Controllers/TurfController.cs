using Application.DTOs;
using Application.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Persistence.Interfaces;
using Application.Common.Result;

namespace TurfBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurfController(ITurfService turfService) : ControllerBase
{
    private readonly ITurfService _turfService = turfService;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] TurfQueryParameters query)
    {
        var result = await _turfService.GetAllTurfAsync(query);

        return Ok(ApiResponse<PagedResult<TurfResponseDto>>.SuccessResponse(result, "Turfs retrieved successfully"));
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