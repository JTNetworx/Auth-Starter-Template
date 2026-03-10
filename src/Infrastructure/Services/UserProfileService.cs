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

    public UserProfileService(
        UserManager<User> userManager,
        ApplicationDbContext dbContext,
        IProfileImageStore profileImageStore,
        IDateTimeProvider dateTime)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _profileImageStore = profileImageStore;
        _dateTime = dateTime;
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
}
