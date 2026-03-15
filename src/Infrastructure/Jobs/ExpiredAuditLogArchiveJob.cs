using Infrastructure.Options;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// Purges audit log records older than the configured retention period.
/// Runs on the schedule defined in Quartz:AuditLogCleanup:CronSchedule.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ExpiredAuditLogArchiveJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<QuartzSettings> _settings;
    private readonly ILogger<ExpiredAuditLogArchiveJob> _logger;

    public ExpiredAuditLogArchiveJob(
        IServiceScopeFactory scopeFactory,
        IOptions<QuartzSettings> settings,
        ILogger<ExpiredAuditLogArchiveJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var retentionDays = _settings.Value.AuditLogCleanup.RetentionDays;
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation(
            "ExpiredAuditLogArchiveJob starting. Deleting audit logs older than {Cutoff:O}.",
            cutoff);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var deleted = await db.AuditLogs
                .Where(l => l.Timestamp < cutoff)
                .ExecuteDeleteAsync(context.CancellationToken);

            context.Result = $"Purged {deleted} audit log record(s).";
            _logger.LogInformation(
                "ExpiredAuditLogArchiveJob complete. Deleted {Count} audit log record(s).", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpiredAuditLogArchiveJob failed.");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}
