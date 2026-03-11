using Domain.Users;
using System.Security.Claims;

namespace Application.Services;

public interface ITokenService
{
    Task<string> GenerateAccessToken(User user, IList<string> roles, Guid sessionId);
    RefreshToken GenerateRefreshToken(string userId, string? ipAddress = null, string? userAgent = null);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
