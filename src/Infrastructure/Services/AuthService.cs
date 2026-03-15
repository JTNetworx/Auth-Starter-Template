using Application;
using Application.DTOs.Auth;
using Application.Services;
using Domain.Users;
using Infrastructure.Persistance.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SharedKernel;
using System.Text;
using System.Text.Encodings.Web;

namespace Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly ITokenRepository _tokenRepository;
    private readonly IAppEmailSender _emailSender;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _dateTime;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditLogService _audit;

    public AuthService(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        ITokenRepository tokenRepository,
        IAppEmailSender emailSender,
        IUnitOfWork uow,
        IDateTimeProvider dateTime,
        IHttpContextAccessor httpContextAccessor,
        IAuditLogService audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _tokenRepository = tokenRepository;
        _emailSender = emailSender;
        _uow = uow;
        _dateTime = dateTime;
        _httpContextAccessor = httpContextAccessor;
        _audit = audit;
    }

    private string? GetClientIp() =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public async Task<Result> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return Result.Failure("Passwords do not match");

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            UserName = dto.UserName,
            Email = dto.Email,
            CreatedAtUtc = _dateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        var roleExists = await _roleManager.RoleExistsAsync("User");
        if (!roleExists)
            await _roleManager.CreateAsync(new IdentityRole("User"));
        var roleResult = await _userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        // Send confirmation email. Email is best-effort — failure does not fail registration.
        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(confirmToken));
        await _emailSender.SendEmailConfirmationAsync(user.Email!, user.UserName!, user.Id, encodedToken);

        await _audit.RecordAsync(user.Id, AuditActions.Register, entityType: "User", entityId: user.Id);
        return Result.Success();
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName)
                   ?? await _userManager.FindByEmailAsync(dto.UserName);

        if (user is null)
            return Result.Failure<LoginResultDto>("Invalid credentials");

        if (await _userManager.IsLockedOutAsync(user))
            return Result.Failure<LoginResultDto>("Account is locked. Try again later.");

        if (!await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            await _userManager.AccessFailedAsync(user);
            await _audit.RecordAsync(user.Id, AuditActions.LoginFailed, details: new { Reason = "InvalidPassword" });
            return Result.Failure<LoginResultDto>("Invalid credentials");
        }

        if (!user.EmailConfirmed)
        {
            await _audit.RecordAsync(user.Id, AuditActions.LoginFailed, details: new { Reason = "EmailNotConfirmed" });
            return Result.Failure<LoginResultDto>("Email address has not been confirmed");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        // If 2FA is enabled, pause here — the client must supply a TOTP code before tokens are issued.
        if (user.TwoFactorEnabled)
            return Result.Success(new LoginResultDto(RequiresTwoFactor: true, UserId: user.Id, AccessToken: null, RefreshToken: null));

        var tokens = await IssueTokensAsync(user);
        return Result.Success(new LoginResultDto(RequiresTwoFactor: false, UserId: null, tokens.AccessToken, tokens.RefreshToken));
    }

    public async Task<Result<TokenDto>> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _tokenRepository.GetRefreshTokenAsync(refreshToken);

        if (storedToken is null)
            return Result.Failure<TokenDto>("Invalid refresh token");

        // A revoked token being reused is a strong signal of theft.
        // Revoke all sessions for this user as a security response.
        if (storedToken.RevokedAtUtc.HasValue)
        {
            await _tokenRepository.DeleteAllRefreshTokensForUserAsync(storedToken.UserId);
            await _uow.SaveChangesAsync();
            return Result.Failure<TokenDto>("Security violation detected. All sessions have been revoked. Please log in again.");
        }

        if (storedToken.IsExpired)
            return Result.Failure<TokenDto>("Refresh token has expired. Please log in again.");

        var user = storedToken.User;
        if (user is null)
            return Result.Failure<TokenDto>("User account no longer exists");

        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, GetClientIp(), GetUserAgent());
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = await _tokenService.GenerateAccessToken(user, roles, newRefreshToken.Id);

        // Atomic: revoke old token and issue new one in a single transaction.
        await _uow.BeginTransactionAsync();
        storedToken.RevokedAtUtc = _dateTime.UtcNow;
        await _tokenRepository.UpdateRefreshTokenAsync(storedToken);
        await _tokenRepository.SaveRefreshTokenAsync(newRefreshToken);
        await _uow.CommitTransactionAsync();

        return Result.Success(new TokenDto(newAccessToken, newRefreshToken.Token));
    }

    public async Task<Result> LogoutAsync(string refreshToken)
    {
        var storedToken = await _tokenRepository.GetRefreshTokenAsync(refreshToken);

        if (storedToken is null)
            return Result.Failure("Token not found");

        if (!storedToken.IsActive)
            return Result.Success();

        storedToken.RevokedAtUtc = _dateTime.UtcNow;
        await _tokenRepository.UpdateRefreshTokenAsync(storedToken);
        await _uow.SaveChangesAsync();
        await _audit.RecordAsync(storedToken.UserId, AuditActions.Logout);
        return Result.Success();
    }

    public async Task<Result> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        string decodedToken;
        try { decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)); }
        catch { return Result.Failure("Invalid confirmation token"); }

        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _audit.RecordAsync(userId, AuditActions.EmailConfirmed, entityType: "User", entityId: userId);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmNewPassword)
            return Result.Failure("New passwords do not match");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _audit.RecordAsync(userId, AuditActions.PasswordChanged);
        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        // Always return success to prevent user enumeration
        if (user is null || !user.EmailConfirmed)
            return Result.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        await _emailSender.SendPasswordResetAsync(user.Email!, user.UserName!, encodedToken);
        await _audit.RecordAsync(user.Id, AuditActions.PasswordResetRequested);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmNewPassword)
            return Result.Failure("Passwords do not match");

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Result.Failure("Invalid request");

        string decodedToken;
        try { decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token)); }
        catch { return Result.Failure("Invalid reset token"); }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await _audit.RecordAsync(user.Id, AuditActions.PasswordReset);
        return Result.Success();
    }

    public async Task<Result<List<SessionDto>>> GetSessionsAsync(string userId, Guid? currentSessionId)
    {
        var sessions = await _tokenRepository.GetActiveSessionsForUserAsync(userId);
        var dtos = sessions.Select(t => new SessionDto(
            t.Id,
            t.IpAddress,
            t.UserAgent,
            t.CreatedAtUtc,
            t.LastUsedUtc,
            IsCurrent: t.Id == currentSessionId
        )).ToList();

        return Result.Success(dtos);
    }

    public async Task<Result> RevokeSessionAsync(Guid sessionId, string userId)
    {
        var token = await _tokenRepository.GetActiveTokenByIdAsync(sessionId, userId);
        if (token is null)
            return Result.Failure("Session not found or already revoked");

        token.RevokedAtUtc = _dateTime.UtcNow;
        await _tokenRepository.UpdateRefreshTokenAsync(token);
        await _uow.SaveChangesAsync();
        await _audit.RecordAsync(userId, AuditActions.SessionRevoked, entityType: "Session", entityId: sessionId.ToString());
        return Result.Success();
    }

    public async Task<Result> RevokeAllOtherSessionsAsync(string userId, Guid? currentSessionId)
    {
        await _tokenRepository.DeleteAllRefreshTokensForUserAsync(userId, excludeId: currentSessionId);
        await _uow.SaveChangesAsync();
        await _audit.RecordAsync(userId, AuditActions.AllSessionsRevoked);
        return Result.Success();
    }

    // ── Two-Factor Authentication ─────────────────────────────────────────────

    public async Task<Result<TwoFactorSetupDto>> GetTwoFactorSetupAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<TwoFactorSetupDto>("User not found");

        // Load or generate the authenticator key.
        var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var sharedKey = FormatAuthenticatorKey(unformattedKey!);
        var email = await _userManager.GetEmailAsync(user) ?? user.UserName ?? userId;
        var authenticatorUri = GenerateAuthenticatorUri(email, unformattedKey!);

        return Result.Success(new TwoFactorSetupDto(sharedKey, authenticatorUri, string.Empty));
    }

    public async Task<Result> EnableTwoFactorAsync(string userId, TwoFactorCodeDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        var code = dto.Code.Replace(" ", "").Replace("-", "");
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
            return Result.Failure("Invalid verification code. Please check your authenticator app and try again.");

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        await _audit.RecordAsync(userId, AuditActions.TwoFactorEnabled);
        return Result.Success();
    }

    public async Task<Result> DisableTwoFactorAsync(string userId, TwoFactorCodeDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        var code = dto.Code.Replace(" ", "").Replace("-", "");
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
            return Result.Failure("Invalid verification code. Please check your authenticator app and try again.");

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        await _audit.RecordAsync(userId, AuditActions.TwoFactorDisabled);
        return Result.Success();
    }

    public async Task<Result<TokenDto>> VerifyTwoFactorAsync(TwoFactorVerifyDto dto)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user is null || !user.TwoFactorEnabled)
            return Result.Failure<TokenDto>("Invalid request");

        if (await _userManager.IsLockedOutAsync(user))
            return Result.Failure<TokenDto>("Account is locked. Try again later.");

        var code = dto.Code.Replace(" ", "").Replace("-", "");
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, code);

        if (!isValid)
        {
            await _userManager.AccessFailedAsync(user);
            await _audit.RecordAsync(user.Id, AuditActions.LoginFailed, details: new { Reason = "InvalidTotpCode" });
            return Result.Failure<TokenDto>("Invalid verification code.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        var tokens = await IssueTokensAsync(user);
        return Result.Success(tokens);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Issues access + refresh tokens and updates LastLoginUtc atomically.</summary>
    private async Task<TokenDto> IssueTokensAsync(User user)
    {
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id, GetClientIp(), GetUserAgent());
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = await _tokenService.GenerateAccessToken(user, roles, newRefreshToken.Id);

        await _uow.BeginTransactionAsync();
        user.LastLoginUtc = _dateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _tokenRepository.SaveRefreshTokenAsync(newRefreshToken);
        await _uow.CommitTransactionAsync();

        return new TokenDto(accessToken, newRefreshToken.Token);
    }

    /// <summary>Formats a raw base32 key into space-separated 4-char groups for readability.</summary>
    private static string FormatAuthenticatorKey(string unformattedKey)
    {
        var result = new StringBuilder();
        for (var i = 0; i < unformattedKey.Length; i++)
        {
            if (i > 0 && i % 4 == 0) result.Append(' ');
            result.Append(char.ToUpperInvariant(unformattedKey[i]));
        }
        return result.ToString();
    }

    /// <summary>Builds the otpauth:// URI that authenticator apps scan or import.</summary>
    private static string GenerateAuthenticatorUri(string email, string unformattedKey) =>
        $"otpauth://totp/{UrlEncoder.Default.Encode("Auth Starter")}:{UrlEncoder.Default.Encode(email)}" +
        $"?secret={unformattedKey}&issuer={UrlEncoder.Default.Encode("Auth Starter")}&digits=6";
}
