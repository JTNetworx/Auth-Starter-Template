namespace Domain.DomainExceptions;

public class EmailNotConfirmedException : DomainException
{
    public EmailNotConfirmedException(string message = "Email address has not been confirmed")
        : base(message) { }
}
