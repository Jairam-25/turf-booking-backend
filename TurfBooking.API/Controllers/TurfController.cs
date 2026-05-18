using Application.DTOs;
using Application.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _turfService.DeleteTurfAsync(id);

        if (!result)
            return NotFound(new
            {
                message = "Turf not found"
            });

        return Ok(new
        {
            message = "Turf deleted successfully"
        });
    }
}