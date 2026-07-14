namespace GameHub.Application.Interfaces.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool VerifyPassword(string password, string passwordHash);
}
