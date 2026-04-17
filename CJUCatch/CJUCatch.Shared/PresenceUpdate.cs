namespace CJUCatch.Shared;

public sealed record PresenceUpdate(
    double PositionX,
    double PositionY,
    PresenceState State);
