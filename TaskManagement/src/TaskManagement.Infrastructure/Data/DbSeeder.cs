using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        await context.Database.EnsureCreatedAsync();

        const string adminEmail = "admin@gmail.com";
        var adminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == adminEmail);

        if (!adminExists)
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = "System Admin",
                Email = adminEmail,
                PasswordHash = passwordService.Hash("Admin@123"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}
