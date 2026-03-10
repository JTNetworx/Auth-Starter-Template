namespace Application.DTOs.Auth;

public record TokenDto(string AccessToken, string RefreshToken);