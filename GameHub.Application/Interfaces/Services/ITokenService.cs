using GameHub.Domain.Entities;

namespace GameHub.Application.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}