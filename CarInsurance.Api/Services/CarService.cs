using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists)
        {
            throw new KeyNotFoundException($"Car {carId} not found");
        }

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }


    public async Task<ClaimDto> AddClaimAsync(long carId, DateOnly claimDate, string description, decimal amount)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists)
            throw new KeyNotFoundException($"Car {carId} not found");

        var claim = new InsuranceClaim
        {
            CarId = carId,
            ClaimDate = claimDate,
            Description = description,
            Amount = amount
        };

        _db.InsuranceClaim.Add(claim);
        await _db.SaveChangesAsync();

        return new ClaimDto(claim.ClaimDate, claim.Description, claim.Amount);
    }
    public async Task<List<CarHistoryEntryDto>> GetCarHistoryAsync(long carId)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists)
            throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new CarHistoryEntryDto
            {
                Type = "Policy",
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Description = p.Provider,
                Amount = null
            })
            .Concat(_db.InsuranceClaim
                .Where(c => c.CarId == carId)
                .Select(c => new CarHistoryEntryDto
                {
                    Type = "Claim",
                    StartDate = c.ClaimDate,
                    EndDate = null,
                    Description = c.Description,
                    Amount = c.Amount
                })
            )
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }

}
