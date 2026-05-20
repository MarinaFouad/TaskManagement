using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users
            .IgnoreQueryFilters() // needed for login - let service handle deleted check
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IReadOnlyList<User>> GetAllAsync() =>
        await _context.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        return user;
    }

    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string email) =>
        await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email);
}
