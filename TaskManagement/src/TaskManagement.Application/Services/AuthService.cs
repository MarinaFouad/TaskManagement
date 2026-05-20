using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Exceptions;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;

    public AuthService(IUnitOfWork uow, IPasswordService passwordService, ITokenService tokenService)
    {
        _uow = uow;
        _passwordService = passwordService;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists to avoid duplicates
        if (await _uow.Users.ExistsAsync(request.Email))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        // Create new user with hashed password
        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(), // normalize email
            PasswordHash = _passwordService.Hash(request.Password),
            Role = TaskManagement.Domain.Enums.UserRole.User // default role
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        // Generate tokens immediately after registration
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Try to find user by email
        var user = await _uow.Users.GetByEmailAsync(request.Email.ToLowerInvariant())
            ?? throw new UnauthorizedException("Invalid email or password.");

        // Prevent login if account is soft-deleted
        if (user.IsDeleted)
            throw new UnauthorizedException("Account has been deactivated.");

        // Validate password
        if (!_passwordService.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // Get stored refresh token
        var storedToken = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        // Make sure token is still valid
        if (!storedToken.IsActive)
            throw new UnauthorizedException("Refresh token is expired or revoked.");

        // Load user linked to token
        var user = await _uow.Users.GetByIdAsync(storedToken.UserId)
            ?? throw new UnauthorizedException("User not found.");

        // Revoke old token before issuing a new one
        storedToken.IsRevoked = true;
        await _uow.RefreshTokens.UpdateAsync(storedToken);

        return await GenerateAuthResponseAsync(user);
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        // Find token to revoke
        var token = await _uow.RefreshTokens.GetByTokenAsync(refreshToken)
            ?? throw new NotFoundException("Refresh token not found.");

        token.IsRevoked = true;

        await _uow.RefreshTokens.UpdateAsync(token);
        await _uow.SaveChangesAsync();
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        // Generate access + refresh tokens
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        // Store refresh token in DB
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync();

        // Return everything needed by client
        return new AuthResponse(
            accessToken,
            refreshTokenValue,
            DateTime.UtcNow.AddHours(1),
            MapToDto(user));
    }

    // Simple mapper (kept private to avoid duplication)
    private static UserDto MapToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
}