using Application.Services;
using System.Collections.Concurrent;

namespace Infrastructure.Jobs;

/// <summary>
/// In-memory store for the most recent execution of each job.
/// Populated by JobAuditListener on every execution (scheduled or manual).
/// Cleared on app restart — the audit log is the durable record.
/// </summary>
public sealed class JobHistoryCache : IJobHistoryCache
{
    private readonly ConcurrentDictionary<string, JobRunInfo> _history = new();

    public void Record(string jobName, JobRunInfo info) => _history[jobName] = info;
    public JobRunInfo? Get(string jobName) => _history.GetValueOrDefault(jobName);
}
