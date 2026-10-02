using Microsoft.AspNetCore.Mvc; 
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GameHub.Application.UserCases;
using GameHub.Application.DTOs.Users;


namespace GameHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly GetUserProfileUseCase _getUserProfileUseCase;

        public UserController(GetUserProfileUseCase getUserProfileUseCase)
        {
            _getUserProfileUseCase = getUserProfileUseCase;
        }

     
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {  

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            UserProfileDTO userProfileDto = await _getUserProfileUseCase.ExecuteAsync(userId);

            return Ok(userProfileDto);
            
        }

    }

}
