using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarInsurance.Tests.Services;

public class CarServiceTests
{
    private readonly DbContextOptions<AppDbContext> _dbContextOptions;

    public CarServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
    }

    [Fact]
    public async Task IsInsuranceValidAsync_CarExistsWithValidPolicy_ReturnsTrue()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var carId = 1L;
        var testDate = new DateOnly(2024, 6, 1);

        var owner = new Owner { Id = 1, Name = "Test Owner", Email = "test@example.com" };
        context.Owners.Add(owner);

        context.Cars.Add(new Car
        {
            Id = carId,
            Vin = "TESTVIN123456789",
            Make = "Test",
            Model = "Model",
            YearOfManufacture = 2020,
            OwnerId = 1
        });

        context.Policies.Add(new InsurancePolicy
        {
            Id = 1,
            CarId = carId,
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31),
            Provider = "Test Provider"
        });

        await context.SaveChangesAsync();

        var service = new CarService(context);

        // Act
        var result = await service.IsInsuranceValidAsync(carId, testDate);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsInsuranceValidAsync_CarExistsWithoutPolicy_ReturnsFalse()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var carId = 1L;
        var testDate = new DateOnly(2024, 6, 1);

        var owner = new Owner { Id = 1, Name = "Test Owner", Email = "test@example.com" };
        context.Owners.Add(owner);

        context.Cars.Add(new Car
        {
            Id = carId,
            Vin = "TESTVIN123456789",
            Make = "Test",
            Model = "Model",
            YearOfManufacture = 2020,
            OwnerId = 1
        });

        await context.SaveChangesAsync();

        var service = new CarService(context);

        // Act
        var result = await service.IsInsuranceValidAsync(carId, testDate);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsInsuranceValidAsync_CarDoesNotExist_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var carId = 999L; // Non-existent car
        var testDate = new DateOnly(2024, 6, 1);

        var owner = new Owner { Id = 1, Name = "Test Owner", Email = "test@example.com" };
        context.Owners.Add(owner);

        context.Cars.Add(new Car
        {
            Id = 1,
            Vin = "TESTVIN123456789",
            Make = "Test",
            Model = "Model",
            YearOfManufacture = 2020,
            OwnerId = 1
        });

        await context.SaveChangesAsync();

        var service = new CarService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.IsInsuranceValidAsync(carId, testDate));
    }

    [Fact]
    public async Task IsInsuranceValidAsync_PolicyBoundaryDates_ReturnsCorrectResults()
    {
        // Arrange
        using var context = new AppDbContext(_dbContextOptions);
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var carId = 1L;
        var policyStart = new DateOnly(2024, 1, 1);
        var policyEnd = new DateOnly(2024, 12, 31);

        var owner = new Owner { Id = 1, Name = "Test Owner", Email = "test@example.com" };
        context.Owners.Add(owner);

        context.Cars.Add(new Car
        {
            Id = carId,
            Vin = "TESTVIN123456789",
            Make = "Test",
            Model = "Model",
            YearOfManufacture = 2020,
            OwnerId = 1
        });

        context.Policies.Add(new InsurancePolicy
        {
            Id = 1,
            CarId = carId,
            StartDate = policyStart,
            EndDate = policyEnd,
            Provider = "Test Provider"
        });

        await context.SaveChangesAsync();

        var service = new CarService(context);

        // Test day before policy starts
        var resultBefore = await service.IsInsuranceValidAsync(carId, policyStart.AddDays(-1));
        Assert.False(resultBefore);

        // Test first day of policy
        var resultStart = await service.IsInsuranceValidAsync(carId, policyStart);
        Assert.True(resultStart);

        // Test last day of policy
        var resultEnd = await service.IsInsuranceValidAsync(carId, policyEnd);
        Assert.True(resultEnd);

        // Test day after policy ends
        var resultAfter = await service.IsInsuranceValidAsync(carId, policyEnd.AddDays(1));
        Assert.False(resultAfter);
    }
}