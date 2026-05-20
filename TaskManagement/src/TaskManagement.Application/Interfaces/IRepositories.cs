using TaskManagement.Domain.Entities;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<IReadOnlyList<User>> GetAllAsync();
    Task<User> AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsAsync(string email);
}

public interface ITaskRepository
{
    Task<UserTask?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<UserTask>> GetByUserIdAsync(Guid userId);
    Task<UserTask> AddAsync(UserTask task);
    Task UpdateAsync(UserTask task);
    Task<bool> ExistsDuplicateTodayAsync(Guid userId, string title, DateTime date);
    Task<IReadOnlyList<UserTask>> GetPendingUnprocessedAsync();
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId);
}

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ITaskRepository Tasks { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
