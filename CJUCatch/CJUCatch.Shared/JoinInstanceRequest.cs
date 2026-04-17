namespace CJUCatch.Shared;

public sealed record JoinInstanceRequest(
    string InstanceCode,
    string DisplayName,
    string SessionId);
