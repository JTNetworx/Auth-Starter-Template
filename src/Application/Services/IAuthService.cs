using Application.DTOs.Auth;
using SharedKernel;

namespace Application.Services;

public interface IAuthService
{
    Task<Result> RegisterAsync(RegisterDto dto);
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto);
    Task<Result<TokenDto>> RefreshTokenAsync(string refreshToken);
    Task<Result> LogoutAsync(string refreshToken);
    Task<Result> ConfirmEmailAsync(string userId, string token);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    Task<Result> ForgotPasswordAsync(string email);
    Task<Result> ResetPasswordAsync(ResetPasswordDto dto);

    // Session management
    Task<Result<List<SessionDto>>> GetSessionsAsync(string userId, Guid? currentSessionId);
    Task<Result> RevokeSessionAsync(Guid sessionId, string userId);
    Task<Result> RevokeAllOtherSessionsAsync(string userId, Guid? currentSessionId);

    // Two-factor authentication
    Task<Result<TwoFactorSetupDto>> GetTwoFactorSetupAsync(string userId);
    Task<Result> EnableTwoFactorAsync(string userId, TwoFactorCodeDto dto);
    Task<Result> DisableTwoFactorAsync(string userId, TwoFactorCodeDto dto);
    Task<Result<TokenDto>> VerifyTwoFactorAsync(TwoFactorVerifyDto dto);
}
