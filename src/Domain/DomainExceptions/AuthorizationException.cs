namespace Domain.DomainExceptions;

public class AuthorizationException : DomainException
{
    public AuthorizationException(string message = "You are not authorized to perform this action") : base(message) { }
}
