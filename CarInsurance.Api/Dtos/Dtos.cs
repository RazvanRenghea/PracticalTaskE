namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);
public record ClaimDto(DateOnly ClaimDate, string Description, decimal Amount);

public class CarHistoryEntryDto
{
    public string Type { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
}

