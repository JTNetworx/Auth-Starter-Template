using System.Net.Http.Json;

namespace WebClient.Services;

/// <summary>
/// Calls the backend /api/auth/* endpoints.
/// Uses the "public" HttpClient (no auth handler — login/register don't need a token).
/// 2FA setup/enable/disable use the "api" client (bearer token required).
/// </summary>
public sealed class AuthApiService : IAuthApiService
{
    private readonly HttpClient _public;
    private readonly HttpClient _authorized;
    private readonly TokenStorageService _tokenStorage;
    private readonly JwtAuthenticationStateProvider _authStateProvider;

    public AuthApiService(
        IHttpClientFactory httpClientFactory,
        TokenStorageService tokenStorage,
        JwtAuthenticationStateProvider authStateProvider)
    {
        _public = httpClientFactory.CreateClient("public");
        _authorized = httpClientFactory.CreateClient("api");
        _tokenStorage = tokenStorage;
        _authStateProvider = authStateProvider;
    }

    // Covers both login shapes: 2FA-required ({ requiresTwoFactor, userId })
    // and normal login ({ accessToken, refreshToken }).
    private record LoginApiResponse(
        bool RequiresTwoFactor, string? UserId,
        string? AccessToken, string? RefreshToken);

    public async Task<ApiResult<LoginResult>> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _public.PostAsJsonAsync("auth/login", request);
            if (!response.IsSuccessStatusCode)
                return ApiResult<LoginResult>.Failure(await ReadErrorAsync(response));

            // ReadFromJsonAsync uses Web defaults (camelCase, case-insensitive) — covers both shapes.
            var parsed = await response.Content.ReadFromJsonAsync<LoginApiResponse>();
            if (parsed is null)
                return ApiResult<LoginResult>.Failure("Invalid response from server.");

            if (parsed.RequiresTwoFactor)
                return ApiResult<LoginResult>.Success(new LoginResult(true, parsed.UserId, null));

            if (parsed.AccessToken is null || parsed.RefreshToken is null)
                return ApiResult<LoginResult>.Failure("Invalid response from server.");

            var tokens = new TokenDto(parsed.AccessToken, parsed.RefreshToken);
            await _tokenStorage.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
            _authStateProvider.NotifyStateChanged();
            return ApiResult<LoginResult>.Success(new LoginResult(false, null, tokens));
        }
        catch (Exception ex)
        {
            return ApiResult<LoginResult>.Failure($"Login failed: {ex.Message}");
        }
    }

    public async Task<ApiResult> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _public.PostAsJsonAsync("auth/register", request);
            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure(await ReadErrorAsync(response));

            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            return ApiResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    public async Task<ApiResult> LogoutAsync(string refreshToken)
    {
        try
        {
            await _public.PostAsJsonAsync("auth/logout", refreshToken);
            await _tokenStorage.ClearAsync();
            _authStateProvider.NotifyStateChanged();
            return ApiResult.Success();
        }
        catch
        {
            // Always clear locally even if the server call fails
            await _tokenStorage.ClearAsync();
            _authStateProvider.NotifyStateChanged();
            return ApiResult.Success();
        }
    }

    public async Task<ApiResult> ForgotPasswordAsync(string email)
    {
        try
        {
            await _public.PostAsJsonAsync("auth/forgot-password", new { Email = email });
            return ApiResult.Success(); // always 200 per backend spec
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var response = await _public.PostAsJsonAsync("auth/reset-password", request);
            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure(await ReadErrorAsync(response));
            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            var response = await _authorized.PostAsJsonAsync("auth/change-password", request);
            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure(await ReadErrorAsync(response));
            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task StoreTokensAsync(TokenDto tokens)
    {
        await _tokenStorage.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
        _authStateProvider.NotifyStateChanged();
    }

    // ── Two-Factor Authentication ─────────────────────────────────────────────

    public async Task<ApiResult<TwoFactorSetupInfo>> GetTwoFactorSetupAsync()
    {
        try
        {
            var response = await _authorized.GetAsync("auth/2fa/setup");
            if (!response.IsSuccessStatusCode)
                return ApiResult<TwoFactorSetupInfo>.Failure(await ReadErrorAsync(response));

            var setup = await response.Content.ReadFromJsonAsync<TwoFactorSetupInfo>();
            if (setup is null) return ApiResult<TwoFactorSetupInfo>.Failure("Invalid response from server.");
            return ApiResult<TwoFactorSetupInfo>.Success(setup);
        }
        catch (Exception ex)
        {
            return ApiResult<TwoFactorSetupInfo>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> EnableTwoFactorAsync(string code)
    {
        try
        {
            var response = await _authorized.PostAsJsonAsync("auth/2fa/enable", new { Code = code });
            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure(await ReadErrorAsync(response));
            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult> DisableTwoFactorAsync(string code)
    {
        try
        {
            var response = await _authorized.PostAsJsonAsync("auth/2fa/disable", new { Code = code });
            if (!response.IsSuccessStatusCode)
                return ApiResult.Failure(await ReadErrorAsync(response));
            return ApiResult.Success();
        }
        catch (Exception ex)
        {
            return ApiResult.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<TokenDto>> VerifyTwoFactorAsync(TwoFactorVerifyRequest request)
    {
        try
        {
            var response = await _public.PostAsJsonAsync("auth/2fa/verify", new { request.UserId, request.Code });
            if (!response.IsSuccessStatusCode)
                return ApiResult<TokenDto>.Failure(await ReadErrorAsync(response));

            var tokens = await response.Content.ReadFromJsonAsync<TokenDto>();
            if (tokens is null) return ApiResult<TokenDto>.Failure("Invalid response from server.");

            await _tokenStorage.SetTokensAsync(tokens.AccessToken, tokens.RefreshToken);
            _authStateProvider.NotifyStateChanged();
            return ApiResult<TokenDto>.Success(tokens);
        }
        catch (Exception ex)
        {
            return ApiResult<TokenDto>.Failure(ex.Message);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return body?.Error ?? response.ReasonPhrase ?? "An error occurred.";
        }
        catch
        {
            return response.ReasonPhrase ?? "An error occurred.";
        }
    }

    private record ErrorResponse(string? Error);
}
