using GameHub.Application.Interfaces.Services;
using GameHub.Application.Interfaces.Repositories;


namespace GameHub.Application.UserCases;

public class LoginUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUserUseCase(IUserRepository userRepository, 
        IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }
    




}