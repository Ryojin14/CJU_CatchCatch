namespace CJUCatch.Shared;

public sealed record SpeechBubbleUpdate(
    string SessionId,
    string? Text);
