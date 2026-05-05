namespace JwtApi.Application.Configuration;

public class JwtOptions
{
    public const string Section = "Jwt";

    public int ExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
