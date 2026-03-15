using Infrastructure.Options;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// Deletes refresh tokens that are both expired/revoked AND older than the configured
/// retention period. Tokens within the retention window are kept for audit trails.
/// Runs on the schedule defined in Quartz:TokenCleanup:CronSchedule.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ExpiredTokenCleanupJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<QuartzSettings> _settings;
    private readonly ILogger<ExpiredTokenCleanupJob> _logger;

    public ExpiredTokenCleanupJob(
        IServiceScopeFactory scopeFactory,
        IOptions<QuartzSettings> settings,
        ILogger<ExpiredTokenCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var retentionDays = _settings.Value.TokenCleanup.RetentionDays;
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation(
            "ExpiredTokenCleanupJob starting. Deleting tokens expired/revoked before {Cutoff:O}.",
            cutoff);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var deleted = await db.RefreshTokens
                .Where(t =>
                    // Expired naturally and past retention
                    (t.ExpiresUtc < cutoff) ||
                    // Explicitly revoked and past retention
                    (t.RevokedAtUtc != null && t.RevokedAtUtc < cutoff))
                .ExecuteDeleteAsync(context.CancellationToken);

            context.Result = $"Deleted {deleted} refresh token(s).";
            _logger.LogInformation(
                "ExpiredTokenCleanupJob complete. Deleted {Count} token(s).", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpiredTokenCleanupJob failed.");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
