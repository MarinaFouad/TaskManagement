using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Data.Repositories;

namespace TaskManagement.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public ITaskRepository Tasks { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        Users = new UserRepository(context);
        Tasks = new TaskRepository(context);
        RefreshTokens = new RefreshTokenRepository(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
