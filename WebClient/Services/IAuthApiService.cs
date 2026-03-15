namespace WebClient.Services;

public interface IAuthApiService
{
    Task<ApiResult<LoginResult>> LoginAsync(LoginRequest request);
    Task<ApiResult> RegisterAsync(RegisterRequest request);
    Task<ApiResult> LogoutAsync(string refreshToken);
    Task<ApiResult> ForgotPasswordAsync(string email);
    Task<ApiResult> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request);
    Task StoreTokensAsync(TokenDto tokens);

    // Two-factor authentication
    Task<ApiResult<TwoFactorSetupInfo>> GetTwoFactorSetupAsync();
    Task<ApiResult> EnableTwoFactorAsync(string code);
    Task<ApiResult> DisableTwoFactorAsync(string code);
    Task<ApiResult<TokenDto>> VerifyTwoFactorAsync(TwoFactorVerifyRequest request);
}
