using Application;
using Application.DTOs.User;
using Application.Services;
using Domain.Users;
using Infrastructure.Persistance;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IProfileImageStore _profileImageStore;
    private readonly IDateTimeProvider _dateTime;
    private readonly IAuditLogService _audit;

    public UserProfileService(
        UserManager<User> userManager,
        ApplicationDbContext dbContext,
        IProfileImageStore profileImageStore,
        IDateTimeProvider dateTime,
        IAuditLogService audit)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _profileImageStore = profileImageStore;
        _dateTime = dateTime;
        _audit = audit;
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(string userId)
    {
        var profile = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                EmailConfirmed = u.EmailConfirmed,
                TwoFactorEnabled = u.TwoFactorEnabled,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                DateOfBirth = u.DateOfBirth,
                Street = u.Street,
                Street2 = u.Street2,
                City = u.City,
                State = u.State,
                PostalCode = u.PostalCode,
                Country = u.Country != null ? u.Country.Name : null,
                ProfileImageUrl = u.ProfileImageUrl,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginUtc = u.LastLoginUtc
            })
            .FirstOrDefaultAsync();

        if (profile is null)
            return Result.Failure<UserProfileDto>("User not found");

        return Result.Success(profile);
    }

    public async Task<Result<UserProfileDto>> UpdateProfileAsync(string userId, UpdateUserProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<UserProfileDto>("User not found");

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.DateOfBirth = dto.DateOfBirth;
        user.Street = dto.Street;
        user.Street2 = dto.Street2;
        user.City = dto.City;
        user.State = dto.State;
        user.PostalCode = dto.PostalCode;
        user.CountryId = dto.CountryId;
        user.UpdatedAtUtc = _dateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<UserProfileDto>(errors);
        }

        await _audit.RecordAsync(userId, AuditActions.ProfileUpdated);
        return await GetProfileAsync(userId);
    }

    public async Task<Result<string>> UpdateProfileImageAsync(string userId, Stream imageStream, string contentType)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<string>("User not found");

        var url = await _profileImageStore.SaveAsync(userId, imageStream, contentType);

        user.ProfileImageUrl = url;
        user.UpdatedAtUtc = _dateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure<string>(errors);
        }

        return Result.Success(url);
    }

    public async Task<Result> DeleteProfileImageAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        await _profileImageStore.DeleteAsync(userId);

        user.ProfileImageUrl = null;
        user.UpdatedAtUtc = _dateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    public Task<(byte[] Data, string ContentType)?> GetProfileImageBytesAsync(string userId) =>
        _profileImageStore.GetAsync(userId);

    public async Task<Result> DeleteAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure("User not found");

        // Audit before deletion — UserId on the record will be set to NULL by DB cascade
        await _audit.RecordAsync(userId, AuditActions.AccountDeleted, entityType: "User", entityId: userId);

        // Delete refresh tokens (UserTokens table)
        await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId)
            .ExecuteDeleteAsync();

        // Delete passkey credentials
        await _dbContext.PasskeyCredentials
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync();

        // Delete profile image from backing store
        await _profileImageStore.DeleteAsync(userId);

        // Delete the Identity user — cascades AspNetUserRoles, AspNetUserClaims, etc.
        // AuditLog.UserId is set to NULL automatically via DeleteBehavior.SetNull on the FK.
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        return Result.Success();
    }

    public async Task<Result<AccountExportDto>> ExportDataAsync(string userId)
    {
        var user = await _dbContext.Users
            .Include(u => u.Country)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Result.Failure<AccountExportDto>("User not found");

        var profile = new ProfileExport(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            user.TwoFactorEnabled,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Street,
            user.Street2,
            user.City,
            user.State,
            user.PostalCode,
            user.Country?.Name,
            user.CreatedAtUtc,
            user.LastLoginUtc);

        var sessions = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId
                     && t.RevokedAtUtc == null
                     && t.ExpiresUtc > DateTime.UtcNow)
            .OrderByDescending(t => t.LastUsedUtc)
            .Select(t => new SessionExport(t.Id, t.CreatedAtUtc, t.IpAddress, t.UserAgent, t.LastUsedUtc))
            .ToListAsync();

        var passkeys = await _dbContext.PasskeyCredentials
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PasskeyExport(p.Name ?? "Passkey", p.CreatedAt.UtcDateTime))
            .ToListAsync();

        var auditHistory = await _dbContext.AuditLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new AuditLogExport(l.Action, l.EntityType, l.Timestamp, l.IpAddress, l.Details))
            .ToListAsync();

        await _audit.RecordAsync(userId, AuditActions.AccountDataExported);

        return Result.Success(new AccountExportDto(profile, sessions, passkeys, auditHistory, DateTimeOffset.UtcNow));
    }
}
