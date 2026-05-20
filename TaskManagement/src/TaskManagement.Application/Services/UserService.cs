using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;

    public UserService(IUnitOfWork uow, IPasswordService passwordService)
    {
        _uow = uow;
        _passwordService = passwordService;
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        // Load current user or fail
        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        return MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync()
    {
        var users = await _uow.Users.GetAllAsync();

        // Simple projection to DTO
        return users.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // Avoid duplicate emails
        if (await _uow.Users.ExistsAsync(request.Email))
            throw new ConflictException($"Email '{request.Email}' is already registered.");

        // Try parse role, fallback to User if invalid
        var role = Enum.TryParse<UserRole>(request.Role, true, out var parsedRole)
            ? parsedRole
            : UserRole.User;

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordService.Hash(request.Password),
            Role = role
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await _uow.Users.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        // Prevent double delete
        if (user.IsDeleted)
            throw new ConflictException("User is already deleted.");

        // Soft delete instead of removing from DB
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();
    }

    private static UserDto MapToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
}