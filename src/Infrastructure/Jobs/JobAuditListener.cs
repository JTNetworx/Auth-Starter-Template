using Application;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// Runs after every job execution (scheduled or manual).
/// Writes to the in-memory JobHistoryCache (for the admin UI) and
/// to the persistent AuditLog (for the audit log viewer).
/// </summary>
public sealed class JobAuditListener : IJobListener
{
    private readonly IJobHistoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobAuditListener> _logger;

    public string Name => "JobAuditListener";

    public JobAuditListener(
        IJobHistoryCache cache,
        IServiceScopeFactory scopeFactory,
        ILogger<JobAuditListener> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct)
        => Task.CompletedTask;

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct)
        => Task.CompletedTask;

    public async Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken ct)
    {
        var jobName = context.JobDetail.Key.Name;
        var startedAt = context.FireTimeUtc;
        var completedAt = DateTimeOffset.UtcNow;
        var durationMs = (long)(completedAt - startedAt).TotalMilliseconds;

        var status = jobException is null ? "Success" : "Failed";
        var details = jobException is null
            ? context.Result?.ToString()
            : jobException.InnerException?.Message ?? jobException.Message;

        // TriggeredBy is set in the JobDataMap when an admin manually fires the job
        var triggeredBy = context.MergedJobDataMap.ContainsKey("TriggeredBy")
            ? context.MergedJobDataMap.GetString("TriggeredBy")
            : null;

        // Update in-memory cache so GetJobsAsync returns current data immediately
        _cache.Record(jobName, new JobRunInfo(startedAt, completedAt, durationMs, status, details, triggeredBy));

        // Write durable audit record
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var audit = scope.ServiceProvider.GetRequiredService<IAuditLogService>();

            var action = status == "Success" ? AuditActions.JobCompleted : AuditActions.JobFailed;
            var auditDetails = new
            {
                DurationMs = durationMs,
                Result = details,
                TriggeredBy = triggeredBy ?? "scheduler"
            };

            await audit.RecordAsync(
                userId: triggeredBy,   // null for scheduled runs
                action: action,
                entityType: "Job",
                entityId: jobName,
                details: auditDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit record for job {JobName}.", jobName);
        }
    }
}
