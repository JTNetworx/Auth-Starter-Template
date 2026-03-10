namespace Domain.DomainExceptions;

public class AuthenticationException : DomainException
{
    public AuthenticationException(string message = "Authentication Failed") : base(message) { }
}
