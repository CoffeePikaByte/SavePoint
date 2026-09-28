
using GameHub.Application.Interfaces.Repositories;
using GameHub.Domain.Entities;
using GameHub.Application.DTOs.Users;
using GameHub.Application.Exceptions;

namespace GameHub.Application.UserCases;

public class GetUserProfileUseCase
{
    private readonly IUserRepository _userRepository;


    public GetUserProfileUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    } 

    public async Task<UserProfileDTO> ExecuteAsync(String userId)
    {
        Guid id = Guid.Parse(userId);

        User user = await _userRepository.GetByIdAsync(id);

        if(user is null)    
        {
            throw new UserNotFoundException("Usuario no encontrado.");  
        }

        return new UserProfileDTO
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        }; 

    } 


}