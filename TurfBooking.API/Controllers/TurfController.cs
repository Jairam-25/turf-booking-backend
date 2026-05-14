using Application.DTOs;
using Application.Model;
using Microsoft.AspNetCore.Mvc;
using Persistence.Interfaces;

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

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTurfDto dto)
    {
        var result = await _turfService.CreateTurfAsync(dto);

        return Ok(result);
    }
}