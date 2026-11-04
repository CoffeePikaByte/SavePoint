using GameHub.Application.UserCases;
using Microsoft.AspNetCore.Mvc;
using GameHub.Domain.Entities;


namespace GameHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly CreateGameUseCase _createGameUseCase;
        private readonly GetGameUseCase _getGameUseCase;

        public GameController(CreateGameUseCase createGameUseCase, GetGameUseCase getGameUseCase)
        {
            _createGameUseCase = createGameUseCase;
            _getGameUseCase = getGameUseCase;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var games = await _getGameUseCase.ExecuteAsync();

            return Ok(games);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int externalId)
        {
            Game game = await _createGameUseCase.ExecuteAsync(externalId);

            return Created(string.Empty, game);

        }

    }

}
