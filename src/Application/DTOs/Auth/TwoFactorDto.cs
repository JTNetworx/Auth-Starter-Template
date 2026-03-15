using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

/// <summary>
/// Returned by GET /api/auth/2fa/setup.
/// Contains the shared key, authenticator URI, and a pre-rendered QR code PNG.
/// </summary>
public record TwoFactorSetupDto(
    string SharedKey,
    string AuthenticatorUri,
    string QrCodeBase64);

/// <summary>
/// Sent to POST /api/auth/2fa/enable and POST /api/auth/2fa/disable.
/// The user must provide their current TOTP code to confirm they have access.
/// </summary>
public record TwoFactorCodeDto(
    [Required][StringLength(8)] string Code);

/// <summary>
/// Sent to POST /api/auth/2fa/verify after a login that required 2FA.
/// </summary>
public record TwoFactorVerifyDto(
    [Required] string UserId,
    [Required][StringLength(8)] string Code);

/// <summary>
/// Returned by POST /api/auth/login.
/// When RequiresTwoFactor is true the client must call POST /api/auth/2fa/verify.
/// When RequiresTwoFactor is false, AccessToken and RefreshToken are populated.
/// </summary>
public record LoginResultDto(
    bool RequiresTwoFactor,
    string? UserId,
    string? AccessToken,
    string? RefreshToken);
