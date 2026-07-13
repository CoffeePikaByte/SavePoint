using Microsoft.AspNetCore.Mvc;
using GameHub.Application.UserCases;
using GameHub.API.Models;

namespace GameHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RegisterUserCase _registerUserCase;

        public AuthController(RegisterUserCase registerUserCase)
        {
            _registerUserCase = registerUserCase;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserRequest request)
        {
            try
            {
                await _registerUserCase.ExecuteAsync(
                    request.UserName,
                    request.Email,
                    request.Password);

                return Created(string.Empty, new
                {
                    message = "User created successfully."
                });
            }   
            catch (Exception ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }

}