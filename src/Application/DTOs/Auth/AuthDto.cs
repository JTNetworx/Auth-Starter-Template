using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

public record LoginDto(
    [Required][StringLength(256)] string UserName,
    [Required][StringLength(256)] string Password);

public record RegisterDto(
    [Required][StringLength(100)] string FirstName,
    [Required][StringLength(100)] string LastName,
    [Required][StringLength(256)] string UserName,
    [Required][EmailAddress][StringLength(256)] string Email,
    [Required][StringLength(256, MinimumLength = 8)] string Password,
    [Required] string ConfirmPassword);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required][StringLength(256, MinimumLength = 8)] string NewPassword,
    [Required] string ConfirmNewPassword);

public record ForgotPasswordDto(
    [Required][EmailAddress][StringLength(256)] string Email);

public record ResetPasswordDto(
    [Required][EmailAddress][StringLength(256)] string Email,
    [Required] string Token,
    [Required][StringLength(256, MinimumLength = 8)] string NewPassword,
    [Required] string ConfirmNewPassword);
