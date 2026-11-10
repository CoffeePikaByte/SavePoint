using System.ComponentModel.DataAnnotations;

namespace GameHub.API.Models;

public class AddLibraryGameRequest : IValidatableObject
{
    public Guid GameId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (GameId == Guid.Empty)
            yield return new ValidationResult("A non-empty gameId is required.", new[] { nameof(GameId) });
    }
}
