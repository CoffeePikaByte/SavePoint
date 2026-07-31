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
        private readonly LoginUserUseCase _loginUserUseCase;

        public AuthController(
            RegisterUserCase registerUserCase,
            LoginUserUseCase loginUserUseCase)
        {
            _registerUserCase = registerUserCase;
            _loginUserUseCase = loginUserUseCase;
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

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginUserRequest request)
        {
            try
            {
                var token = await _loginUserUseCase.ExecuteAsync(request.Email, request.Password);

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

    }

}