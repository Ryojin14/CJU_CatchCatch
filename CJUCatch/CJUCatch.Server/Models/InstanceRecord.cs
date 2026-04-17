namespace CJUCatch.Server.Models;

internal sealed class InstanceRecord
{
    public required string InstanceCode { get; init; }
    public Dictionary<string, ParticipantRecord> Participants { get; } = new(StringComparer.Ordinal);
}
