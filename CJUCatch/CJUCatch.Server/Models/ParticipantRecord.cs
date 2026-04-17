using CJUCatch.Shared;

namespace CJUCatch.Server.Models;

internal sealed class ParticipantRecord
{
    public required string SessionId { get; init; }
    public required string DisplayName { get; set; }
    public required string ConnectionId { get; set; }
    public PresenceState State { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
}
