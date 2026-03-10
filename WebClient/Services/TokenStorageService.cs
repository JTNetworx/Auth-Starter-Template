using Microsoft.JSInterop;

namespace WebClient.Services;

/// <summary>
/// Stores JWT access and refresh tokens in localStorage via JS interop.
/// Tokens are also cached in memory to avoid redundant JS calls.
/// </summary>
public sealed class TokenStorageService
{
    private readonly IJSRuntime _js;

    private string? _accessToken;
    private string? _refreshToken;

    public TokenStorageService(IJSRuntime js) => _js = js;

    public async ValueTask<string?> GetAccessTokenAsync()
    {
        _accessToken ??= await _js.InvokeAsync<string?>("localStorage.getItem", "auth_access_token");
        return _accessToken;
    }

    public async ValueTask<string?> GetRefreshTokenAsync()
    {
        _refreshToken ??= await _js.InvokeAsync<string?>("localStorage.getItem", "auth_refresh_token");
        return _refreshToken;
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_access_token", accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", "auth_refresh_token", refreshToken);
    }

    public async Task ClearAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_access_token");
        await _js.InvokeVoidAsync("localStorage.removeItem", "auth_refresh_token");
    }
}
