namespace CJUCatch.Shared;

public sealed record ParticipantSnapshot(
    string SessionId,
    string DisplayName,
    double PositionX,
    double PositionY,
    PresenceState State);
