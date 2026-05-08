using Microsoft.AspNetCore.Mvc;
using TurfBooking.Application.Interfaces;
using TurfBooking.Domain.Entities;

namespace TurfBooking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TurfController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public TurfController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var turfs = await _unitOfWork.Turfs.GetAllAsync();

        return Ok(turfs);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Turf turf)
    {
        await _unitOfWork.Turfs.AddAsync(turf);

        await _unitOfWork.SaveAsync();

        return Ok(turf);
    }
}