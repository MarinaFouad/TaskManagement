using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Services;

/// <summary>
/// BCrypt-based password hashing. BCrypt is self-salting; no manual salt needed.
/// </summary>
public class BcryptPasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
