using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebClient.Services;

/// <summary>
/// Delegating handler that attaches the Bearer token to every outgoing request.
/// On 401, attempts a silent token refresh and retries the original request once.
/// On refresh failure, clears tokens and notifies auth state changed (user is signed out).
/// </summary>
public sealed class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly TokenStorageService _tokenStorage;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthHttpMessageHandler(
        TokenStorageService tokenStorage,
        JwtAuthenticationStateProvider authStateProvider,
        IHttpClientFactory httpClientFactory)
    {
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var accessToken = await _tokenStorage.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshTokenAsync(ct);
            if (refreshed)
            {
                // Clone and retry — HttpRequestMessage can only be sent once
                var retryRequest = await CloneRequestAsync(request, ct);
                var newToken = await _tokenStorage.GetAccessTokenAsync();
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                response = await base.SendAsync(retryRequest, ct);
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken)) return false;

        try
        {
            // Use a separate "public" client (no auth handler) to call the refresh endpoint
            using var client = _httpClientFactory.CreateClient("public");
            var response = await client.PostAsJsonAsync("auth/refresh", refreshToken, ct);

            if (!response.IsSuccessStatusCode)
            {
                await _tokenStorage.ClearAsync();
                _authStateProvider.NotifyStateChanged();
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenDto>(cancellationToken: ct);
            if (tokens is null)
            {
                await _tokenStorage.ClearAsync();
                _authStateProvider.NotifyStateChanged();
                return false;
            }

            await _tokenStorage.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
            _authStateProvider.NotifyStateChanged();
            return true;
        }
        catch
        {
            await _tokenStorage.ClearAsync();
            _authStateProvider.NotifyStateChanged();
            return false;
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        if (original.Content is not null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync(ct);
            clone.Content = new ByteArrayContent(bytes);

            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
