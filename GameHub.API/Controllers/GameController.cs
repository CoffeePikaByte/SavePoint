using GameHub.Application.UserCases;
using GameHub.API.Models;


namespace GameHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly CreateGameUseCase _createGameUseCase;

        public GameController(CreateGameUseCase createGameUseCase)
        {
            _createGameUseCase = createGameUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGameRequest request)
        {
            await _createGameUseCase.ExecuteAsync(
                request.ExternalId,
                request.Title,
                request.CoverUrl,
                request.ReleaseDate
            );

            return Created(string.Empty, new 
            {
                message = "Game add successfully."
            });

        }

    }

}
