using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace WebClient.Services;

/// <summary>
/// Custom AuthenticationStateProvider that parses JWT tokens stored in localStorage.
/// Replaces the OIDC-based provider. No server round-trips for auth state checks.
/// </summary>
public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly TokenStorageService _tokenStorage;

    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public JwtAuthenticationStateProvider(TokenStorageService tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(accessToken))
            return _anonymous;

        // Check expiry without a server call
        if (IsTokenExpired(accessToken))
            return _anonymous;

        var claims = ParseClaimsFromJwt(accessToken);
        // "role" is the short claim name written by JwtSecurityTokenHandler; specify it
        // as the roleClaimType so ClaimsPrincipal.IsInRole and [Authorize(Roles=...)] work.
        var identity = new ClaimsIdentity(claims, "jwt", nameType: "unique_name", roleType: "role");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>
    /// Call after login/register/refresh to push the new auth state to all subscribers.
    /// </summary>
    public void NotifyStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    // ── JWT Parsing ───────────────────────────────────────────────────────────

    private static bool IsTokenExpired(string jwt)
    {
        var claims = ParseClaimsFromJwt(jwt);
        var expClaim = claims.FirstOrDefault(c => c.Type == "exp")?.Value;
        if (!long.TryParse(expClaim, out var exp)) return true;
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp;
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return [];

            var payload = parts[1];
            var jsonBytes = Base64UrlDecode(payload);
            var kvp = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
            if (kvp is null) return [];

            var claims = new List<Claim>();
            foreach (var (key, value) in kvp)
            {
                // Arrays (e.g. roles) become multiple claims
                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in value.EnumerateArray())
                        claims.Add(new Claim(key, el.ToString()));
                }
                else
                {
                    claims.Add(new Claim(key, value.ToString()));
                }
            }
            return claims;
        }
        catch
        {
            return [];
        }
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var padded = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded
        };

        return Convert.FromBase64String(padded);
    }
}
