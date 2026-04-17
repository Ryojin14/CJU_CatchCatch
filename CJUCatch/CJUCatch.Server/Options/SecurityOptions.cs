namespace CJUCatch.Server.Options;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public long MaxHttpRequestBytes { get; init; } = 16 * 1024;
    public long MaxHubMessageBytes { get; init; } = 16 * 1024;
    public int AttemptWindowSeconds { get; init; } = 60;
    public int CreateAttemptsPerWindow { get; init; } = 8;
    public int JoinAttemptsPerWindow { get; init; } = 20;
}
