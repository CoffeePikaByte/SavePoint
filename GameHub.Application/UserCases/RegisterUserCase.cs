using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;
using GameHub.Application.Interfaces.Services;

namespace GameHub.Application.UserCases;

public class RegisterUserCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task ExecuteAsync(
        string userName,
        string email, 
        string password)
    {
        var existingUser = await _userRepository.GetByEmailAsync(email);

        var passwordHash = _passwordHasher.Hash(password);

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