using System.Text.Json;
using System.Text.Json.Serialization;
using GameHub.Application.DTOs.Games;
using GameHub.Domain.Enums;

namespace GameHub.API.Models;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class UpdateLibraryGameRequest
{
    private GameStatus? _status;
    private int? _hoursPlayed;
    // Presence is transport metadata, not a writable JSON property.
    internal bool HasStatus { get; private set; }
    internal bool HasHoursPlayed { get; private set; }

    public GameStatus? Status
    {
        get => _status;
        set
        {
            if (HasStatus) throw new JsonException("Status must appear only once.");
            HasStatus = true;
            _status = value;
        }
    }

    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public int? HoursPlayed
    {
        get => _hoursPlayed;
        set
        {
            if (HasHoursPlayed) throw new JsonException("Hours played must appear only once.");
            HasHoursPlayed = true;
            _hoursPlayed = value;
        }
    }

    public UpdateLibraryGameCommand ToCommand() => new(HasStatus, Status, HasHoursPlayed, HoursPlayed);
}
