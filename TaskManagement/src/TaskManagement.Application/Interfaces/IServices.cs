using TaskManagement.Application.DTOs;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeTokenAsync(string refreshToken);
}

public interface IUserService
{
    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync();
    Task<UserDto> CreateUserAsync(CreateUserRequest request);
    Task DeleteUserAsync(Guid userId); // soft delete
}

public interface ITaskService
{
    Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskRequest request);
    Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid userId);
    Task<IReadOnlyList<TaskDto>> GetAllTasksAsync(Guid userId);
    Task<TaskDto> UpdateTaskStatusAsync(Guid taskId, Guid userId, UpdateTaskStatusRequest request);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
}

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITaskQueue
{
    void Enqueue(Guid taskId);
    bool TryDequeue(out Guid taskId);
}
