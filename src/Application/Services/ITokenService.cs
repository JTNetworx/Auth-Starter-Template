using Domain.Users;
using System.Security.Claims;

namespace Application.Services;

public interface ITokenService
{
    Task<string> GenerateAccessToken(User user, IList<string> roles);
    RefreshToken GenerateRefreshToken(string userId);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
