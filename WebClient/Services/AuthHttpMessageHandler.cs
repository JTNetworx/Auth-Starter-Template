using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WebClient.Services;

/// <summary>
/// Delegating handler that attaches the Bearer token to every outgoing request.
/// On 401, attempts a silent token refresh and retries the original request once.
/// On refresh failure, clears tokens, notifies auth state changed, and navigates
/// to /login so the user is immediately redirected rather than seeing an error.
/// A semaphore ensures only one refresh flight runs at a time — concurrent 401s reuse
/// the result of the in-progress refresh rather than triggering a second rotation.
/// </summary>
public sealed class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly TokenStorageService _tokenStorage;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NavigationManager _nav;

    // Prevents concurrent 401 handlers from each trying to rotate the same refresh token.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthHttpMessageHandler(
        TokenStorageService tokenStorage,
        JwtAuthenticationStateProvider authStateProvider,
        IHttpClientFactory httpClientFactory,
        NavigationManager nav)
    {
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
        _httpClientFactory = httpClientFactory;
        _nav = nav;
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
            var refreshed = await TryRefreshTokenAsync(accessToken, ct);
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

    private async Task<bool> TryRefreshTokenAsync(string? tokenThatGot401, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            // Double-check: if another concurrent request already refreshed while we were
            // waiting, the stored access token will differ from the one that got the 401.
            // In that case, skip the HTTP round-trip — the caller will retry with the new token.
            var currentToken = await _tokenStorage.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(currentToken) && currentToken != tokenThatGot401)
                return true;

            var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken)) return false;

            // Use a separate "public" client (no auth handler) to call the refresh endpoint
            using var client = _httpClientFactory.CreateClient("public");
            var response = await client.PostAsJsonAsync("auth/refresh", refreshToken, ct);

            if (!response.IsSuccessStatusCode)
            {
                await _tokenStorage.ClearAsync();
                _authStateProvider.NotifyStateChanged();
                NavigateToLogin();
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenDto>(cancellationToken: ct);
            if (tokens is null)
            {
                await _tokenStorage.ClearAsync();
                _authStateProvider.NotifyStateChanged();
                NavigateToLogin();
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
            NavigateToLogin();
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void NavigateToLogin()
    {
        var returnUrl = Uri.EscapeDataString(_nav.Uri);
        _nav.NavigateTo($"/login?returnUrl={returnUrl}", replace: true);
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
