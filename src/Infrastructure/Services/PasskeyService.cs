using Application.DTOs.Auth;
using Application.Services;
using Domain.Users;
using Infrastructure.Persistance.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SharedKernel;

namespace Infrastructure.Services;

public sealed class PasskeyService : IPasskeyService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ITokenRepository _tokenRepository;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _dateTime;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PasskeyService(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ITokenService tokenService,
        ITokenRepository tokenRepository,
        IUnitOfWork uow,
        IDateTimeProvider dateTime,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
        _tokenRepository = tokenRepository;
        _uow = uow;
        _dateTime = dateTime;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? GetClientIp() =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public async Task<Result<string>> BeginRegistrationAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<string>("User not found");

        var entity = new PasskeyUserEntity
        {
            Id = user.Id,
            Name = user.UserName!,
            DisplayName = user.Email ?? user.UserName!
        };

        // Generates creation options JSON and stores the challenge in the TwoFactorUserIdScheme cookie
        var optionsJson = await _signInManager.MakePasskeyCreationOptionsAsync(entity);
        return Result.Success(optionsJson);
    }

    public async Task<Result> CompleteRegistrationAsync(string userId, string credentialJson)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        // Reads challenge from cookie, verifies attestation
        var attestation = await _signInManager.PerformPasskeyAttestationAsync(credentialJson);
        if (!attestation.Succeeded)
            return Result.Failure(attestation.Failure?.Message ?? "Passkey registration failed");

        var result = await _userManager.AddOrUpdatePasskeyAsync(user, attestation.Passkey);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    public async Task<Result<string>> BeginLoginAsync(string? userName)
    {
        User? user = null;
        if (!string.IsNullOrWhiteSpace(userName))
        {
            user = await _userManager.FindByNameAsync(userName)
                   ?? await _userManager.FindByEmailAsync(userName);
        }

        // Passing null enables the discoverable credential (usernameless) flow
        var optionsJson = await _signInManager.MakePasskeyRequestOptionsAsync(user);
        return Result.Success(optionsJson);
    }

    public async Task<Result<TokenDto>> CompleteLoginAsync(string credentialJson)
    {
        // Reads challenge from cookie, verifies assertion, returns the matched user and updated passkey
        var assertion = await _signInManager.PerformPasskeyAssertionAsync(credentialJson);
        if (!assertion.Succeeded)
            return Result.Failure<TokenDto>(assertion.Failure?.Message ?? "Passkey authentication failed");

        var user = assertion.User;

        if (!user.EmailConfirmed)
            return Result.Failure<TokenDto>("Email address has not been confirmed");

        // Persist the updated sign count (replay attack prevention)
        await _userManager.AddOrUpdatePasskeyAsync(user, assertion.Passkey);

        user.LastLoginUtc = _dateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var refreshToken = _tokenService.GenerateRefreshToken(user.Id, GetClientIp(), GetUserAgent());
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = await _tokenService.GenerateAccessToken(user, roles, refreshToken.Id);
        await _tokenRepository.SaveRefreshTokenAsync(refreshToken);
        await _uow.SaveChangesAsync();

        return Result.Success(new TokenDto(accessToken, refreshToken.Token));
    }

    public async Task<Result<List<PasskeyInfoDto>>> GetPasskeysAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<List<PasskeyInfoDto>>("User not found");

        var passkeys = await _userManager.GetPasskeysAsync(user);
        var dtos = passkeys
            .Select(p => new PasskeyInfoDto(
                Convert.ToBase64String(p.CredentialId),
                p.Name,
                p.CreatedAt))
            .ToList();

        return Result.Success(dtos);
    }

    public async Task<Result> RemovePasskeyAsync(string userId, string credentialIdBase64)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        byte[] credentialId;
        try { credentialId = Convert.FromBase64String(credentialIdBase64); }
        catch { return Result.Failure("Invalid credential ID format"); }

        var result = await _userManager.RemovePasskeyAsync(user, credentialId);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }
}
