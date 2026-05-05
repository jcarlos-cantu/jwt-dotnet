using JwtApi.Application.DTOs;
using JwtApi.Application.Interfaces;
using JwtApi.Application.Configuration;
using JwtApi.Domain.Entities;
using JwtApi.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace JwtApi.Application.UseCases;

public class AuthService(
    IUserRepository userRepository,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("El email ya está registrado.");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await userRepository.AddAsync(user);

        return await IssueTokensAsync(user);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Credenciales inválidas.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas.");

        return await IssueTokensAsync(user);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request)
    {
        var user = await userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Refresh token inválido.");

        if (!user.HasValidRefreshToken(request.RefreshToken))
            throw new UnauthorizedAccessException("Refresh token expirado.");

        return await IssueTokensAsync(user);
    }

    private async Task<TokenResponse> IssueTokensAsync(User user)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationDays);

        await userRepository.SaveChangesAsync();

        return new TokenResponse(accessToken, refreshToken);
    }
}
