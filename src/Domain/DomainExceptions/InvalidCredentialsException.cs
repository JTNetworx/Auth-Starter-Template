namespace Domain.DomainExceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException(string message = "Invalid email or password")
        : base(message) { }
}
