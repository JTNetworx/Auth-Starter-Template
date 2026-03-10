using Application.DTOs.User;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IUserProfileService _userProfileService;

    public UsersController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// Returns the full profile of the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMeAsync()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _userProfileService.GetProfileAsync(userId);
        if (result.IsFailure) return NotFound(new { result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user.
    /// </summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMeAsync([FromBody] UpdateUserProfileDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _userProfileService.UpdateProfileAsync(userId, dto);
        if (result.IsFailure) return BadRequest(new { result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Uploads a new profile image for the currently authenticated user.
    /// Accepted: JPEG, PNG, WebP, GIF — max 5 MB.
    /// </summary>
    [HttpPut("me/profile-image")]
    [RequestSizeLimit(MaxImageSizeBytes)]
    public async Task<IActionResult> UploadProfileImageAsync(IFormFile file)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (file is null || file.Length == 0)
            return BadRequest(new { Error = "No file provided." });

        if (file.Length > MaxImageSizeBytes)
            return BadRequest(new { Error = "Image exceeds the 5 MB size limit." });

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedImageTypes.Contains(contentType))
            return BadRequest(new { Error = "Unsupported image type. Allowed: JPEG, PNG, WebP, GIF." });

        await using var stream = file.OpenReadStream();
        var result = await _userProfileService.UpdateProfileImageAsync(userId, stream, contentType);
        if (result.IsFailure) return BadRequest(new { result.Error });

        return Ok(new { url = result.Value });
    }

    /// <summary>
    /// Deletes the profile image of the currently authenticated user.
    /// </summary>
    [HttpDelete("me/profile-image")]
    public async Task<IActionResult> DeleteProfileImageAsync()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _userProfileService.DeleteProfileImageAsync(userId);
        if (result.IsFailure) return BadRequest(new { result.Error });

        return NoContent();
    }

    /// <summary>
    /// Serves a user's profile image from database storage.
    /// Public endpoint — allows img tags to load images without auth headers.
    /// Only used when App:ProfileImageStorage = "Database".
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{userId}/profile-image")]
    public async Task<IActionResult> GetProfileImageAsync(string userId)
    {
        var result = await _userProfileService.GetProfileImageBytesAsync(userId);
        if (result is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=3600";
        return File(result.Value.Data, result.Value.ContentType);
    }

    private string? GetUserId() =>
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
}
