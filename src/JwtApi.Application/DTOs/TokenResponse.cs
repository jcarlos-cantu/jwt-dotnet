namespace JwtApi.Application.DTOs;

public record TokenResponse(string AccessToken, string RefreshToken);
