using System.Security.Claims;
using GameHub.API.Models;
using GameHub.Application.UserCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[ApiController]
[Authorize]
[Route("api/library/games")]
public class LibraryController(AddGameToLibraryUseCase addGameToLibrary) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddLibraryGameRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
            return Unauthorized();

        var result = await addGameToLibrary.ExecuteAsync(userId, request.GameId);
        return Created(string.Empty, result);
    }
}
