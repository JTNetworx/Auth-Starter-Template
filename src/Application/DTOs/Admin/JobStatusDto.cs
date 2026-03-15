namespace Application.DTOs.Admin;

public record JobStatusDto(
    string Name,
    string? Description,
    string TriggerState,
    DateTimeOffset? NextFireTimeUtc,
    DateTimeOffset? PreviousFireTimeUtc,
    string? CronExpression,
    bool IsRunning,
    // Populated from JobHistoryCache (accurate for both scheduled and manual runs)
    DateTimeOffset? LastRunUtc,
    string? LastRunStatus,
    string? LastRunDetails,
    long? LastRunDurationMs,
    string? LastRunTriggeredBy);   // null = cron schedule, userId = manual admin trigger
