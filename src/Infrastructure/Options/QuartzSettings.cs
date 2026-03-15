namespace Infrastructure.Options;

public class QuartzSettings
{
    public const string SectionName = "Quartz";

    public JobOptions TokenCleanup { get; set; } = new();
    public JobOptions AuditLogCleanup { get; set; } = new();

    public class JobOptions
    {
        /// <summary>Quartz cron expression for when the job runs.</summary>
        public string CronSchedule { get; set; } = "0 0 3 * * ?"; // 3am daily

        /// <summary>
        /// Records older than this many days are eligible for deletion.
        /// Applies to both expired/revoked tokens and audit log entries.
        /// </summary>
        public int RetentionDays { get; set; } = 30;
    }
}
