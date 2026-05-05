using FluentAssertions;
using JwtApi.Application.DTOs;
using JwtApi.Application.Interfaces;
using JwtApi.Application.UseCases;
using JwtApi.Domain.Entities;
using JwtApi.Domain.Enums;
using JwtApi.Domain.Repositories;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace JwtApi.Application.Tests.UseCases;

public class AuthServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepository, _tokenService);

        _tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        _tokenService.GenerateRefreshToken().Returns("refresh-token");
    }

    // ─── Register ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsTokenResponse()
    {
        _userRepository.GetByEmailAsync("nuevo@test.com").ReturnsNull();

        var result = await _sut.RegisterAsync(new RegisterRequest("nuevo@test.com", "Password123!"));

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_SavesUserWithHashedPassword()
    {
        _userRepository.GetByEmailAsync("nuevo@test.com").ReturnsNull();

        await _sut.RegisterAsync(new RegisterRequest("nuevo@test.com", "Password123!"));

        await _userRepository.Received(1).AddAsync(Arg.Is<User>(u =>
            u.Email == "nuevo@test.com" &&
            u.PasswordHash != "Password123!" &&
            BCrypt.Net.BCrypt.Verify("Password123!", u.PasswordHash)
        ));
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_SetsRefreshTokenOnUser()
    {
        _userRepository.GetByEmailAsync("nuevo@test.com").ReturnsNull();

        await _sut.RegisterAsync(new RegisterRequest("nuevo@test.com", "Password123!"));

        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u =>
            u.RefreshToken == "refresh-token" &&
            u.RefreshTokenExpiry > DateTime.UtcNow
        ));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsInvalidOperationException()
    {
        var existing = BuildUser("existente@test.com");
        _userRepository.GetByEmailAsync("existente@test.com").Returns(existing);

        var act = () => _sut.RegisterAsync(new RegisterRequest("existente@test.com", "Password123!"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("El email ya está registrado.");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_NeverCallsAddAsync()
    {
        var existing = BuildUser("existente@test.com");
        _userRepository.GetByEmailAsync("existente@test.com").Returns(existing);

        try { await _sut.RegisterAsync(new RegisterRequest("existente@test.com", "x")); } catch { }

        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>());
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenResponse()
    {
        var user = BuildUserWithPassword("user@test.com", "Password123!");
        _userRepository.GetByEmailAsync("user@test.com").Returns(user);

        var result = await _sut.LoginAsync(new LoginRequest("user@test.com", "Password123!"));

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        _userRepository.GetByEmailAsync("noexiste@test.com").ReturnsNull();

        var act = () => _sut.LoginAsync(new LoginRequest("noexiste@test.com", "cualquiera"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = BuildUserWithPassword("user@test.com", "PasswordCorrecta");
        _userRepository.GetByEmailAsync("user@test.com").Returns(user);

        var act = () => _sut.LoginAsync(new LoginRequest("user@test.com", "PasswordIncorrecta"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Credenciales inválidas.");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_UpdatesRefreshTokenOnUser()
    {
        var user = BuildUserWithPassword("user@test.com", "Password123!");
        _userRepository.GetByEmailAsync("user@test.com").Returns(user);

        await _sut.LoginAsync(new LoginRequest("user@test.com", "Password123!"));

        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u =>
            u.RefreshToken == "refresh-token"
        ));
        await _userRepository.Received(1).SaveChangesAsync();
    }

    // ─── Refresh ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_WithValidToken_ReturnsNewTokenResponse()
    {
        var user = BuildUserWithValidRefreshToken("token-valido");
        _userRepository.GetByRefreshTokenAsync("token-valido").Returns(user);

        var result = await _sut.RefreshAsync(new RefreshRequest("token-valido"));

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task RefreshAsync_WithUnknownToken_ThrowsUnauthorizedAccessException()
    {
        _userRepository.GetByRefreshTokenAsync("token-inexistente").ReturnsNull();

        var act = () => _sut.RefreshAsync(new RefreshRequest("token-inexistente"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token inválido.");
    }

    [Fact]
    public async Task RefreshAsync_WithExpiredToken_ThrowsUnauthorizedAccessException()
    {
        var user = BuildUserWithExpiredRefreshToken("token-expirado");
        _userRepository.GetByRefreshTokenAsync("token-expirado").Returns(user);

        var act = () => _sut.RefreshAsync(new RefreshRequest("token-expirado"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token expirado.");
    }

    [Fact]
    public async Task RefreshAsync_WithValidToken_RotatesRefreshToken()
    {
        var user = BuildUserWithValidRefreshToken("token-viejo");
        _userRepository.GetByRefreshTokenAsync("token-viejo").Returns(user);
        _tokenService.GenerateRefreshToken().Returns("token-nuevo");

        await _sut.RefreshAsync(new RefreshRequest("token-viejo"));

        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u =>
            u.RefreshToken == "token-nuevo"
        ));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static User BuildUser(string email) => new()
    {
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
        Role = UserRole.User
    };

    private static User BuildUserWithPassword(string email, string password) => new()
    {
        Email = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = UserRole.User
    };

    private static User BuildUserWithValidRefreshToken(string token) => new()
    {
        Email = "user@test.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
        Role = UserRole.User,
        RefreshToken = token,
        RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
    };

    private static User BuildUserWithExpiredRefreshToken(string token) => new()
    {
        Email = "user@test.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
        Role = UserRole.User,
        RefreshToken = token,
        RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1)
    };
}
