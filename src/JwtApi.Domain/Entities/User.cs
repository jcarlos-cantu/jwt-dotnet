using JwtApi.Domain.Enums;

namespace JwtApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public bool HasValidRefreshToken(string token) =>
        RefreshToken == token && RefreshTokenExpiry > DateTime.UtcNow;
}
