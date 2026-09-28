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
    


    public async Task<string> ExecuteAsync(string email, string password)
    {
        
        var user = await _userRepository.GetByEmailAsync(email);

        if(user is null)    
        {
            throw new Exception("User not found.");  
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(
            password, 
            user.PasswordHash);
        
        if(!isPasswordValid)
        {
            throw new Exception("Password is incorrect.");
        }

        var token = _tokenService.GenerateToken(user);

        return token;
    }



}