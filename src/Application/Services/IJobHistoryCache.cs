namespace Application.Services;

public record JobRunInfo(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    long DurationMs,
    string Status,      // "Success" | "Failed"
    string? Details,    // e.g. "Deleted 10 token(s)"
    string? TriggeredBy // null = scheduled cron, userId = manual admin trigger
);

public interface IJobHistoryCache
{
    void Record(string jobName, JobRunInfo info);
    JobRunInfo? Get(string jobName);
}
