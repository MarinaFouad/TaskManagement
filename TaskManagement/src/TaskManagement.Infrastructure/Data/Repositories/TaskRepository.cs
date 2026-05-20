using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure.Data.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context) => _context = context;

    public async Task<UserTask?> GetByIdAsync(Guid id) =>
        await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<IReadOnlyList<UserTask>> GetByUserIdAsync(Guid userId) =>
        await _context.Tasks
            .Where(t => t.UserId == userId)
            .ToListAsync();

    public async Task<UserTask> AddAsync(UserTask task)
    {
        await _context.Tasks.AddAsync(task);
        return task;
    }

    public Task UpdateAsync(UserTask task)
    {
        _context.Tasks.Update(task);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsDuplicateTodayAsync(Guid userId, string title, DateTime date)
    {
        var nextDay = date.AddDays(1);
        return await _context.Tasks.AnyAsync(t =>
            t.UserId == userId &&
            t.Title == title &&
            t.CreatedAt >= date &&
            t.CreatedAt < nextDay);
    }

    public async Task<IReadOnlyList<UserTask>> GetPendingUnprocessedAsync() =>
        await _context.Tasks
            .Where(t => !t.IsProcessed && t.Status == TaskStatus.Pending)
            .ToListAsync();
}
