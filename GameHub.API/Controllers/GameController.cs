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

        public GameController(CreateGameUseCase createGameUseCase)
        {
            _createGameUseCase = createGameUseCase;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromQuery] int externalId)
        {
            Game game = await _createGameUseCase.ExecuteAsync(externalId);

            return Created(string.Empty, game);

        }

    }

}
