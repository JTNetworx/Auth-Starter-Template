namespace Domain.DomainExceptions;

public class RateLimitExceededException : DomainException
{
    public TimeSpan RetryAfter { get; }

    public RateLimitExceededException(TimeSpan retryAfter)
        : base($"Rate limit exceeded. Try again after {retryAfter.TotalSeconds:F0} seconds")
    {
        RetryAfter = retryAfter;
    }

    public RateLimitExceededException(string message = "Rate limit exceeded")
        : base(message)
    {
        RetryAfter = TimeSpan.FromMinutes(1);
    }
}