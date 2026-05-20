namespace TaskManagement.Application.DTOs;

public record RegisterRequest(string Name, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt);

public record CreateUserRequest(string Name, string Email, string Password, string Role = "User");

public record UpdateUserRequest(string? Name, string? Email);
