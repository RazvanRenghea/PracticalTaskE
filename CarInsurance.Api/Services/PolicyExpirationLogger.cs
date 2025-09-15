using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

public class PolicyExpirationLogger : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PolicyExpirationLogger> _logger;

    public PolicyExpirationLogger(IServiceScopeFactory scopeFactory, ILogger<PolicyExpirationLogger> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task CheckExpiredPoliciesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var utcNow = DateTime.UtcNow;

        var expired = await db.Policies
            .Where(p => p.EndDate <= DateOnly.FromDateTime(utcNow)
                     && p.EndDate >= DateOnly.FromDateTime(utcNow.AddHours(-1)))
            .ToListAsync(cancellationToken);

        foreach (var policy in expired)
        {
            var alreadyLogged = await db.ProcessedExpirations
                .AnyAsync(pe => pe.PolicyId == policy.Id, cancellationToken);

            if (alreadyLogged) continue;

            _logger.LogInformation("Policy {PolicyId} for car {CarId} expired at {EndDate}",
                policy.Id, policy.CarId, policy.EndDate);

            db.ProcessedExpirations.Add(new ProcessedExpiredPolicy
            {
                PolicyId = policy.Id,
                LoggedAt = utcNow
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiredPoliciesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
