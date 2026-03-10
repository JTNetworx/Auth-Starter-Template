namespace Domain.DomainExceptions;

public class ValidationException : DomainException
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string error)
        : base($"Validation failed for {propertyName}: {error}")
    {
        Errors = new Dictionary<string, string[]>
        {
            { propertyName, new[] { error } }
        };
    }

    public IDictionary<string, string[]> Errors { get; }
}
