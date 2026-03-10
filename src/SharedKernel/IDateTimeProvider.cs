namespace SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime UtcToday { get; }
    DateTime UtcMonth { get; }
    DateTime UtcYear { get; }

    string UtcYearString => UtcYear.ToString("yyyy");
    string UtcMonthString => UtcMonth.ToString("MM");
    string UtcDayString => UtcToday.ToString("dd");
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime UtcToday => DateTime.UtcNow.Date;
    public DateTime UtcMonth => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
    public DateTime UtcYear => new DateTime(DateTime.UtcNow.Year, 1, 1);
}
