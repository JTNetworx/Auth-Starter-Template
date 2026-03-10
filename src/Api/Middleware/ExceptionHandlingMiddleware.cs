using Domain.DomainExceptions;
using System.Diagnostics;

namespace Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        switch (ex)
        {
            case ValidationException validation:
                _logger.LogWarning("Validation error: {Message}", ex.Message);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 400,
                    title = "Validation failed",
                    errors = validation.Errors,
                    traceId
                });
                break;

            case AuthenticationException:
            case InvalidCredentialsException:
                _logger.LogWarning("Authentication error: {Message}", ex.Message);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 401,
                    title = ex.Message,
                    traceId
                });
                break;

            case EmailNotConfirmedException:
            case AuthorizationException:
                _logger.LogWarning("Authorization error: {Message}", ex.Message);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 403,
                    title = ex.Message,
                    traceId
                });
                break;

            case NotFoundException notFound:
                _logger.LogWarning("Not found: {Entity} ({Key})", notFound.EntityName, notFound.Key);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 404,
                    title = ex.Message,
                    traceId
                });
                break;

            case RateLimitExceededException rateLimit:
                _logger.LogWarning("Rate limit exceeded. RetryAfter: {RetryAfter}s", rateLimit.RetryAfter.TotalSeconds);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers.RetryAfter = ((int)rateLimit.RetryAfter.TotalSeconds).ToString();
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 429,
                    title = ex.Message,
                    retryAfterSeconds = (int)rateLimit.RetryAfter.TotalSeconds,
                    traceId
                });
                break;

            default:
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    status = 500,
                    title = "An unexpected error occurred",
                    traceId
                });
                break;
        }
    }
}
