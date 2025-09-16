using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsed))
        {
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");
        }

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("cars/{carId:long}/add-claim")]
    public async Task<ActionResult<ClaimDto>> AddClaim(long carId, [FromBody] ClaimDto dto)
    {
        var claim = await _service.AddClaimAsync(carId, dto.ClaimDate, dto.Description, dto.Amount);
        return Ok(claim);

    }
    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<List<CarHistoryEntryDto>>> GetHistory(long carId)
       => Ok(await _service.GetCarHistoryAsync(carId));

}
