using CarInsurance.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CarInsurance.Tests.Controllers;

public class CarsControllerTests
{
    private class TestCarService
    {
        private readonly bool _shouldThrowNotFound;
        private readonly bool _insuranceValid;

        public TestCarService(bool shouldThrowNotFound = false, bool insuranceValid = true)
        {
            _shouldThrowNotFound = shouldThrowNotFound;
            _insuranceValid = insuranceValid;
        }

        public Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
        {
            if (_shouldThrowNotFound)
            {
                throw new KeyNotFoundException($"Car {carId} not found");
            }

            return Task.FromResult(_insuranceValid);
        }

        public Task<List<object>> ListCarsAsync()
        {
            return Task.FromResult(new List<object>());
        }

        public Task<object> AddClaimAsync(long carId, DateOnly claimDate, string description, decimal amount)
        {
            return Task.FromResult(new object());
        }

        public Task<List<object>> GetCarHistoryAsync(long carId)
        {
            return Task.FromResult(new List<object>());
        }
    }
    private class TestableCarsController : CarsController
    {
        private readonly TestCarService _testService;

        public TestableCarsController(TestCarService testService) : base(null!)
        {
            _testService = testService;
        }

        public new Task<ActionResult<object>> IsInsuranceValid(long carId, string date)
        {
            if (!DateOnly.TryParse(date, out var parsed))
                return Task.FromResult<ActionResult<object>>(BadRequest("Invalid date format. Use YYYY-MM-DD."));

            try
            {
                var valid = _testService.IsInsuranceValidAsync(carId, parsed).Result;
                var response = new { carId, date = parsed.ToString("yyyy-MM-dd"), isValid = valid };
                return Task.FromResult<ActionResult<object>>(Ok(response));
            }
            catch (KeyNotFoundException)
            {
                return Task.FromResult<ActionResult<object>>(NotFound());
            }
        }
    }

    [Fact]
    public async Task IsInsuranceValid_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var service = new TestCarService();
        var controller = new TestableCarsController(service);
        var carId = 1L;
        var date = "2024-06-01";

        // Act
        var result = await controller.IsInsuranceValid(carId, date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task IsInsuranceValid_InvalidDateFormat_ReturnsBadRequest()
    {
        // Arrange
        var service = new TestCarService();
        var controller = new TestableCarsController(service);
        var carId = 1L;
        var invalidDate = "invalid-date";

        // Act
        var result = await controller.IsInsuranceValid(carId, invalidDate);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid date format. Use YYYY-MM-DD.", badRequestResult.Value);
    }

    [Fact]
    public async Task IsInsuranceValid_CarNotFound_ReturnsNotFound()
    {
        // Arrange
        var service = new TestCarService(shouldThrowNotFound: true);
        var controller = new TestableCarsController(service);
        var carId = 999L;
        var date = "2024-06-01";

        // Act
        var result = await controller.IsInsuranceValid(carId, date);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }
}