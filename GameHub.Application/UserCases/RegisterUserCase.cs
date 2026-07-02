using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;

namespace GameHub.Application.UserCases;

public class RegisterUserCase
{
    private readonly IUserRepository _userRepository;

    public RegisterUserCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task ExecuteAsync(
        string userName,
        string email, 
        string passwordHash)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);

        if(existingUser is not null)
        {
            throw new Exception("Ya existe un usuario con este email.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            PasswordHash = passwordHash
        };

        await _userRepository.AddAsync(user);
    }


}